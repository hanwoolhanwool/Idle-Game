# 전투(공격) 시스템 구현 계획서

> 대상 프로젝트: Unity 기반 Idle Game
> 작성 기준: 현재 브랜치(main)의 Player 아키텍처(상태 머신 / 스탯 머신 / 버프 시스템)
> 설계 원칙: OOP · SOLID · 확장 가능성 · 테스트 가능성 (CLAUDE.md 준수)

---

## 1. 요구사항 정리

| # | 요구사항 | 설계적 해석 |
|---|----------|-------------|
| R1 | 공격은 **스킬**로 작동한다 | "공격"은 스킬의 한 종류(AttackSkill)일 뿐. 모든 행동을 스킬로 일반화한다. |
| R2 | 스킬을 **6개까지** 저장할 수 있다 | 고정 크기 6의 **슬롯(Loadout)** 개념 도입. |
| R3 | **기본 공격 1개 + 버프/공격 스킬 5개** | 슬롯 0 = 기본 공격(고정), 슬롯 1~5 = 자유 편성. |
| R4 | 버튼을 클릭해 작동한다 | UI 버튼 → `PlayerSkillController.TryUseSkill(slotIndex)` 단방향 호출. |
| R5 | 스킬 사용 중에는 **다른 스킬을 사용할 수 없다** | "시전 중"이라는 **배타적 상태**를 상태 머신으로 표현. 캐스팅이 끝나야 다음 입력 허용. |

### 배타성(R5)의 두 계층
헷갈리기 쉬운 부분이라 먼저 못을 박고 갑니다. "다른 스킬을 못 쓴다"는 상황은 **두 종류**입니다.

1. **시전 중 잠금 (Global Lock)** — 스킬을 쓰는 순간부터 시전(캐스트/모션)이 끝날 때까지는 *어떤 스킬도* 발동 불가. → R5의 핵심. **상태 머신의 상태**로 표현.
2. **개별 쿨다운 (Per-skill Cooldown)** — 시전이 끝났어도 그 스킬만 일정 시간 다시 못 씀. → 스킬별 타이머로 표현.

이 둘을 분리해서 구현하는 것이 핵심 설계 포인트입니다.

---

## 2. 현재 코드 자산 (재사용 대상)

새로 다 만들 필요가 없습니다. 이미 있는 것을 최대한 활용합니다.

| 기존 요소 | 위치 | 전투에서의 역할 |
|-----------|------|-----------------|
| `PlayerStateMachine` / `PlayerStateBase` | `Features/Player/StateMachine/Core` | 시전 중 배타 상태 표현 |
| `PlayerState_Attack` (현재 **빈 껍데기**) | `.../States/PlayerState_Attack.cs` | 스킬 시전 상태로 채워 넣음 |
| `PlayerStateID` (Attack 존재) | `.../Contracts/PlayerStateID.cs` | 필요 시 `Skill` ID 추가 검토 |
| `PlayerCombatController` | `Features/Player/Combat` | 데미지 계산/적용 진입점 |
| `PlayerStatComponent` | `.../Stats/Runtime` | `TrySpendMp`, `ComputeFinalDamagePerHit`, `ComputeDps` |
| `PlayerBuffController` + `BuffDefinition` | `Features/Player/Buffs`, `Data/Definitions` | **버프 스킬의 효과를 그대로 위임** |
| `StatType` (`CooldownReduction`, `Range`, `ProjectileCount`) | `Shared/Enums` | 쿨다운 감소·사거리·투사체 수 반영 |
| `PlayerRoot` (Composition Root) | `Features/Player/Composition` | 새 컨트롤러 생성·주입 지점 |

> 핵심 판단: **버프 스킬 = "버프 발동 트리거"**. 실제 버프 지속/모디파이어 적용은 이미 검증된 `PlayerBuffController.Apply(BuffDefinition)`에 위임한다. 중복 구현하지 않는다(DRY).

---

## 3. 아키텍처 개요

```
[UI 버튼 x6]
     │ TryUseSkill(slotIndex)
     ▼
PlayerSkillController  ────────── (오케스트레이션)
     │  1) 슬롯 조회        SkillLoadout
     │  2) 사용 가능?       SkillCooldownTracker + 상태머신 잠금 + 마나
     │  3) 상태 전이        PlayerStateMachine → Attack(시전) 상태
     │  4) 효과 실행        ISkillEffect.Execute(context)
     ▼
ISkillEffect (전략 패턴)
     ├── AttackSkillEffect ── PlayerCombatController / 타겟 → ApplyDamage
     └── BuffSkillEffect   ── PlayerBuffController.Apply(BuffDefinition)
```

- **데이터**(무엇인가): `SkillDefinition` (ScriptableObject)
- **효과**(무엇을 하는가): `ISkillEffect` 구현체 (전략 패턴 → OCP)
- **편성**(무엇을 들고 있나): `SkillLoadout` (6슬롯)
- **재사용 규칙**(언제 쓸 수 있나): `SkillCooldownTracker`
- **지휘자**(순서 조율): `PlayerSkillController`
- **배타 잠금**(시전 중): `PlayerStateMachine`의 Attack 상태

각 요소가 **하나의 책임(SRP)**만 갖도록 분리한 것이 이 설계의 뼈대입니다.

---

## 4. 클래스 상세 설계

### 4.1 열거형 — `SkillType`
```
enum SkillType { Attack, Buff }
```
- 스킬을 분류하는 최소 정보. 나중에 `Debuff`, `Movement`, `Summon` 등으로 **확장 가능**.
- 위치 제안: `Shared/Enums/SkillType.cs` (기존 enum들과 동일 위치)

### 4.2 데이터 — `SkillDefinition : ScriptableObject`
스킬 한 개의 **불변 설계도**. 인스펙터에서 기획자가 편집하는 데이터 컨테이너.

| 필드 | 타입 | 의미 |
|------|------|------|
| `SkillId` | `string` | 고유 식별자 (쿨다운 추적 키) |
| `DisplayName` | `string` | UI 표기 이름 |
| `Icon` | `Sprite` | 버튼 아이콘 |
| `Type` | `SkillType` | Attack / Buff |
| `ManaCost` | `float` | 소모 마나 |
| `Cooldown` | `float` | 개별 쿨다운(초) |
| `CastTime` | `float` | 시전(잠금) 시간(초). 0이면 즉발 |
| `LinkedBuff` | `BuffDefinition` | (Buff 타입일 때) 발동할 버프 |
| `DamageMultiplier` | `float` | (Attack 타입일 때) 공격력 배수 |

- **SRP**: 로직 없음, 데이터만. `[CreateAssetMenu]`로 에셋 생성.
- **OCP**: 새 스킬을 만들 때 코드 수정 없이 에셋만 추가.
- 위치 제안: `Data/Definitions/SkillDefinition.cs` (`BuffDefinition`과 동일 패턴)

### 4.3 효과(전략) — `ISkillEffect`
스킬이 "무엇을 하는가"를 캡슐화하는 **전략 인터페이스**.
```
interface ISkillEffect {
    void Execute(SkillContext context);
}
```
- `SkillContext`: 효과 실행에 필요한 참조 묶음(값 객체) — `SkillDefinition`, `PlayerCombatController`, `PlayerBuffController`, `PlayerStatComponent`, 타겟 등.
- **DIP**: `PlayerSkillController`는 구체 효과가 아니라 이 인터페이스에 의존.
- **OCP**: 새 효과 = 새 구현 클래스 추가. 컨트롤러는 건드리지 않음.

구현체:
- `AttackSkillEffect` → `PlayerCombatController`(또는 타겟)에 `ComputeFinalDamagePerHit() * DamageMultiplier` 만큼 데미지 적용.
- `BuffSkillEffect` → `PlayerBuffController.Apply(definition.LinkedBuff)` 위임.

> 매핑 방법: `SkillType` → `ISkillEffect`를 `Dictionary`로 등록하는 **팩토리/레지스트리**를 두면, 슬롯에 어떤 스킬이 들어와도 타입만 보고 올바른 효과를 고를 수 있다.

### 4.4 편성 — `SkillLoadout`
6개 슬롯을 관리하는 컬렉션.
```
class SkillLoadout {
    const int SlotCount = 6;
    SkillDefinition GetSlot(int index);
    bool TryEquip(int index, SkillDefinition skill);   // 슬롯 0 고정 규칙 등
}
```
- **SRP**: "어떤 스킬을 어느 슬롯에" 만 담당. 사용 가능 여부/쿨다운은 모름.
- **엣지 케이스**: index 범위(0~5) 검증, 슬롯 0(기본 공격)은 교체 불가 정책 등.

### 4.5 재사용 규칙 — `SkillCooldownTracker`
스킬별 남은 쿨다운을 추적.
```
class SkillCooldownTracker {
    bool IsReady(string skillId);
    void StartCooldown(string skillId, float cooldown);
    void Tick(float deltaTime);
}
```
- **SRP**: 오직 시간만 관리. `Dictionary<string,float>` 사용.
- `CooldownReduction` 스탯을 반영해 실효 쿨다운을 계산(`cooldown * (1 - reduction)`)하는 것은 컨트롤러 또는 여기에서 처리.

### 4.6 지휘자 — `PlayerSkillController`
전체 흐름을 조율하는 **오케스트레이터**. UI가 호출하는 단일 진입점.
```
class PlayerSkillController {
    bool TryUseSkill(int slotIndex);   // UI 버튼이 부르는 유일한 메서드
    void Tick(float deltaTime);        // 쿨다운·시전 타이머 갱신
    bool IsCasting { get; }            // 시전 중 잠금 상태
}
```
`TryUseSkill`의 판정 순서(가드 절, early-return):
1. 슬롯 유효성 → `SkillLoadout.GetSlot`
2. **시전 중 잠금** → `IsCasting == true`면 즉시 실패 (**R5**)
3. **쿨다운** → `SkillCooldownTracker.IsReady`
4. **마나** → `PlayerStatComponent.TrySpendMp`
5. 통과 시 → 상태 머신 Attack 전이 + 시전 타이머 시작 + `ISkillEffect.Execute`
6. 시전 종료 시 → 쿨다운 시작 + Idle/Move 복귀

- **SRP**: 판정과 순서 조율만. 데미지 계산·버프 지속은 하위 객체에 위임.
- **DIP**: 하위 요소를 인터페이스/추상으로 주입받아 테스트 시 목(mock) 교체 가능.

### 4.7 상태 머신 연동 — `PlayerState_Attack`
현재 비어 있는 이 상태를 **시전 상태**로 채운다.
- `Enter()`: 시전 애니메이션 트리거, 이동 입력 차단.
- `Tick()`: 시전 타이머 감소 → 0 되면 종료 콜백.
- `Exit()`: 정리.

> **R5 배타성이 여기서 자연스럽게 보장된다**: 상태가 Attack인 동안 `IsCasting=true`이므로 `TryUseSkill`이 2번 가드에서 전부 막힌다. 상태 머신이 곧 잠금 장치.

> 검토 사항: 공격/버프/이동을 모두 포괄한다면 `PlayerStateID.Attack`을 그대로 쓰기보다 `Skill` 상태를 신설하는 편이 의미가 명확할 수 있음. 1차 구현은 기존 `Attack` 재사용, 이후 리팩터링 여지로 남김.

---

## 5. SOLID 적용 요약

| 원칙 | 적용 지점 |
|------|-----------|
| **SRP** | 데이터(Definition)·효과(Effect)·편성(Loadout)·쿨다운(Tracker)·조율(Controller)을 각각 분리 |
| **OCP** | 새 스킬 종류 = `ISkillEffect` 구현 추가 + 에셋 추가. 컨트롤러 수정 없음 |
| **LSP** | 모든 `ISkillEffect` 구현은 `Execute` 계약을 지켜 서로 대체 가능 |
| **ISP** | UI는 `TryUseSkill`만, 효과는 `Execute`만 의존. 뚱뚱한 인터페이스 없음 |
| **DIP** | 컨트롤러가 구체 효과가 아닌 `ISkillEffect` 추상에 의존 |

---

## 6. 구현 순서 (직접 타이핑용 로드맵)

작은 단위로 나눠 아래 순서대로 작성·검증합니다. 각 단계는 컴파일이 되는 최소 단위입니다.

- **STEP 1 — 데이터 뼈대**
  `SkillType` enum → `SkillDefinition` ScriptableObject 작성.
  검증: 에디터에서 스킬 에셋 1~2개 생성되는지 확인.

- **STEP 2 — 쿨다운 추적기**
  `SkillCooldownTracker` 작성 (순수 C#, MonoBehaviour 아님).
  검증: 단위 테스트 — `StartCooldown` 후 `IsReady`가 false → `Tick` 누적 후 true.

- **STEP 3 — 효과 전략**
  `SkillContext` 값 객체 → `ISkillEffect` → `AttackSkillEffect`, `BuffSkillEffect`.
  검증: `BuffSkillEffect`가 `PlayerBuffController.Apply`를 호출하는지 로그로 확인.

- **STEP 4 — 편성**
  `SkillLoadout` (6슬롯, 슬롯 0 고정 규칙).
  검증: 범위 밖 index, 슬롯 0 교체 시도 등 엣지 케이스.

- **STEP 5 — 지휘자**
  `PlayerSkillController` — 판정 순서·시전 타이머·상태 전이 연결.
  검증: 시전 중 두 번째 `TryUseSkill`이 false 반환(R5).

- **STEP 6 — 상태 연동**
  `PlayerState_Attack` 채우기 + 컨트롤러의 시전 종료 콜백 연결.

- **STEP 7 — 조립**
  `PlayerRoot.Compose()`에 `PlayerSkillController` 생성/주입, `Update()`에 `Tick` 추가.

- **STEP 8 — UI**
  스킬 버튼 6개 → 각 버튼 `onClick` → `TryUseSkill(index)` 바인딩.
  검증: 실기기/에디터에서 버튼 클릭으로 발동, 시전 중 다른 버튼 무반응.

---

## 7. 에러 처리 · 엣지 케이스 체크리스트

- 슬롯 index 범위(0~5) 밖 요청 → 안전 실패(예외 대신 `false` 반환).
- 빈 슬롯(스킬 미장착) 클릭 → 무시.
- `SkillDefinition == null` / `LinkedBuff == null`(Buff 타입인데 버프 미지정) 방어.
- 마나 부족 → 시전 진입 전 차단(마나 선점 실패 시 상태 전이 금지).
- 시전 도중 피격/사망(Hit/Dead 상태 전이) → 시전 강제 중단 및 타이머 정리 정책 결정.
- `CastTime == 0`(즉발 스킬) 처리 — 상태를 한 프레임만 스쳐도 잠금이 올바른지.
- 동일 버프 재시전 → 기존 `RefreshDurationOnReapply` 정책 재사용.

---

## 8. 테스트 전략

- **순수 로직은 MonoBehaviour에서 분리** → EditMode 단위 테스트 대상.
  - `SkillCooldownTracker`: 시간 경과에 따른 준비 상태 전이.
  - `SkillLoadout`: 장착 규칙/범위 검증.
  - `PlayerSkillController`: 목(mock) `ISkillEffect`·목 스탯으로 판정 순서·R5 배타성 검증.
- **효과 클래스**: 목 `PlayerCombatController`/`PlayerBuffController`로 위임 호출 확인.
- **통합(PlayMode)**: 버튼 클릭 → 시전 → 데미지/버프 반영 → 쿨다운 → 재사용 흐름.

---

## 9. 향후 확장 여지 (지금 만들지 않되 막지 않을 것)

- 타겟팅 시스템(적 탐색/사거리 `Range`/투사체 `ProjectileCount`) 분리형 컴포넌트.
- 스킬 레벨/강화, 콤보, 차지 스킬 → `SkillDefinition` 파생 또는 `ISkillEffect` 확장.
- 스킬 슬롯 편성 UI(드래그&드롭)와 저장/로드(세이브 시스템 연동).
- 글로벌 쿨다운(GCD)과 개별 쿨다운의 분리 튜닝.

---

## 10. 신규/수정 파일 요약

| 구분 | 파일 | 위치(제안) |
|------|------|------------|
| 신규 | `SkillType.cs` | `Shared/Enums` |
| 신규 | `SkillDefinition.cs` | `Data/Definitions` |
| 신규 | `SkillContext.cs` | `Features/Player/Skills/Core` |
| 신규 | `ISkillEffect.cs` | `Features/Player/Skills/Contracts` |
| 신규 | `AttackSkillEffect.cs` | `Features/Player/Skills/Effects` |
| 신규 | `BuffSkillEffect.cs` | `Features/Player/Skills/Effects` |
| 신규 | `SkillLoadout.cs` | `Features/Player/Skills/Core` |
| 신규 | `SkillCooldownTracker.cs` | `Features/Player/Skills/Core` |
| 신규 | `PlayerSkillController.cs` | `Features/Player/Skills` |
| 수정 | `PlayerState_Attack.cs` | 시전 상태 구현 |
| 수정 | `PlayerRoot.cs` | 컨트롤러 생성/주입 + Tick |
| 수정 | (선택) `PlayerStateID.cs` | `Skill` 상태 신설 검토 |
| 신규 | UI `SkillButton.cs` | `Features/Player/Presentation` 또는 `UI` |
```

---

## 11. 구현 진행 현황 (2026-07)

> 구현 도중 **게임 장르를 "순수 방치형 + 능동 전투(보스/레이드/PvP) 하이브리드"로 확정**함에 따라, 원래 로드맵(STEP 1~8)을 방치형 방향으로 재구성함(STEP N′). 핵심 판단: 상태 머신을 새로 짜지 않고 **"제어 주체(플레이어/AI)"를 추상화**해 입력 소스만 교체하면 두 모드를 모두 지원한다.

### 11.1 완료 (컴파일·플레이 검증됨)

| 단계 | 산출물 | 위치 | 비고 |
|------|--------|------|------|
| STEP 1 | `SkillType`, `SkillDefinition` | `Shared/Enums`, `Data/Definitions` | `CanMoveWhileCasting` 필드 추가(무빙 공격 지원) |
| STEP 2 | `SkillCooldownTracker` | `Features/Player/Skills` | 순수 C#, `ListPool` 기반 Tick |
| STEP 3 | `IDamageable`, `SkillContext`, `ISkillEffect`, `AttackSkillEffect`, `BuffSkillEffect` | `Skills/Contracts·Core·Effects` | 전략 패턴 |
| STEP 4 | `SkillLoadout` | `Skills/Core` | 6슬롯, 슬롯 0 고정 |
| STEP 5 | `PlayerSkillController` | `Skills` | 판정순서·시전타이머·R5. `TryUseSkill(slot, IDamageable target=null)` |
| STEP 6′ | `IMoveInputSource`(구 `IJoystickInputReader`), `PlayerMovementController` DIP화 | `StateMachine/Contracts`, `Movement` | 입력 소스 교체 가능 |
| STEP 7′ | `Idle⇄Move⇄Attack` 전이, `IPlayerMovementController` 확장 | `StateMachine/States`, `Contracts` | 이동 차단은 컨트롤러가 스킬 속성 기반 |
| STEP 9′ | `ICastGate`, `PlayerStateMachineCastGate`, `PlayerRoot` 조립 | `Skills/Contracts·Adapters`, `Composition` | 시전 진실=상태 머신(이중 진실 제거). 능동 모드 동작 |
| STEP 8′ | `EnemyUnit`, `EnemyRegistry`, `ITargetProvider`/`NearestEnemyTargetProvider`, `AutoBattleInputSource`, `AutoCastController` | `Features/Enemy`, `Input`, `Skills` | 방치 전투 루프(탐색→접근→자동공격→처치→다음, 적 없으면 제자리) |
| 능동 UI | `SkillButton` + `PlayerRoot` 바인딩 | `Presentation`, `Composition` | 버튼 클릭 → `TryUseSkill(slot)`. 기본 공격 동작 확인 |

### 11.2 남은 챕터

> **갱신(2026-07-22)**: 아래 취소선 항목은 이후 리팩터링에서 완료되었다(작업 로그: [refactoring-worklog-2026-07-09.md](../reports/refactoring-worklog-2026-07-09.md)).

- **능동 타겟팅** — 능동 공격 버튼이 실제 적을 때리도록(현재 `target=null`이라 공격 스킬은 로그만).
- ~~모드 전환~~ ✅ **완료(2026-07-09)** — `PlayerInputRouter`로 방치↔능동 입력 소스 런타임 스왑 구현·씬 배선(작업 로그 §C).
- ~~피격 시스템~~ ✅ **완료(2026-07-10)** — `PlayerState_Hit`(경직 0.15s)·`PlayerHitReaction`·`PlayerDeathHandler`·`EnemyAttacker` 구현으로 피격 → `Hit` 전이 + 시전 취소가 동작한다(작업 로그 §8-F, as-built: [combat.md](../specs/player/combat.md)).
- **애니메이션** — `IPlayerAnimationController`(빈 인터페이스) 채우기.
- **본격 적 시스템** — 지속 스폰([m0-close-the-loop-plan.md](m0-close-the-loop-plan.md) §6.1에서 웨이브가 아닌 지속 스폰으로 확정), `EnemyStat` 기반 스탯, 드롭 → M0 ②·③.
- **오프라인 진행** — 방치형 핵심(시간 기반 시뮬레이션) → M1.
- ~~편성 데이터 분리~~ ✅ **완료(2026-07-10)** — `SkillLoadoutConfig` 구현(커밋 `887264b`). 아래 12장은 구현 전 계획의 기록.

---

## 12. 다음 챕터: `SkillLoadoutConfig` (편성 데이터 분리)

### 12.1 문제
현재 슬롯 편성이 `PlayerRoot`의 두 필드(`basicAttack`, `equippedSkills[]`)로 **오브젝트에 박혀** 있다. 재사용·교체·저장이 어렵고, 편성이 플레이어 오브젝트에 종속된다.

### 12.2 목표
6슬롯 편성을 **하나의 ScriptableObject 에셋(`SkillLoadoutConfig`)**으로 분리한다. `PlayerRoot`는 이 config **하나만** 참조한다.

```
[변경 전]  PlayerRoot ─ basicAttack, equippedSkills[]   (오브젝트에 박힘)
[변경 후]  PlayerRoot ─ SkillLoadoutConfig (에셋)  ─ 슬롯 0~5 스킬 지정
```

### 12.3 이점 (SOLID·확장)
- **데이터 분리(SRP)**: 편성=데이터, 조립=`PlayerRoot`. 역할이 갈린다.
- **프리셋(OCP)**: "전사용/마법사용" 편성 에셋을 여러 개 만들어 교체 가능.
- **확장 기반**: 세이브/로드(어떤 config를 썼나), 런타임 편성 UI(config 복제·수정 → `SkillLoadout.TryEquip`)의 토대.

### 12.4 Teaching Mode 로드맵
- **L-A — 데이터**: `SkillLoadoutConfig : ScriptableObject` 작성.
  - 필드: `SkillDefinition basicAttack`(슬롯 0), `SkillDefinition[] equippedSkills`(슬롯 1~5).
  - `[CreateAssetMenu]`로 에셋 생성. 슬롯 0 고정 규칙은 기존 `SkillLoadout` 생성자에서 유지.
  - 검증: 에디터에서 편성 에셋 생성.
- **L-B — 조립 교체**: `PlayerRoot`가 `basicAttack`/`equippedSkills` 대신 `SkillLoadoutConfig loadoutConfig` 하나를 참조하도록 수정. `ComposeSkills`에서 `config.basicAttack`·`config.equippedSkills` 사용.
  - 검증: 컴파일.
- **L-C — 씬**: 편성 에셋 생성 → 슬롯별 스킬 지정 → `PlayerRoot`의 `Loadout Config`에 연결 → 플레이로 각 슬롯 발동 확인.

### 12.5 신규/수정 파일
| 구분 | 파일 | 위치 |
|------|------|------|
| 신규 | `SkillLoadoutConfig.cs` | `Data/Definitions` |
| 수정 | `PlayerRoot.cs` | `basicAttack`/`equippedSkills` → `loadoutConfig` 참조 |

### 12.6 향후 확장 여지 (지금 만들지 않되 막지 않을 것)
- 슬롯 편성 UI(드래그&드롭) → `SkillLoadout.TryEquip`(이미 구현) 재사용.
- 세이브/로드 시 슬롯의 `SkillId`만 직렬화 → 로드 시 `TryEquip`으로 복원.
- 런타임 편성 변경 시 config를 복제(원본 불변 유지)해 수정.
