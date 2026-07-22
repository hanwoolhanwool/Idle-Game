# PlayerRoot 생태계 리팩터링 제안서

> 작성일: 2026-07-09
> 대상: `PlayerRoot`가 조립·구동하는 플레이어 의존 그래프 전체
> 성격: **분석·제안 문서** (코드 변경 없음). 팀이 우선순위를 판단하기 위한 로드맵.
> **후기(2026-07-22)**: §7-D의 "CastGate는 `Attack` 상태 재사용, 전용 `Casting` 상태는 후속"은 이후 뒤집혔다 — `PlayerState_Casting` 신설, `Attack` 상태 제거, CastGate 기본 인자 필수화(커밋 2600858, 상세: [cast-gate-default-argument-mismatch.md](../reports/cast-gate-default-argument-mismatch.md)). §2 의존 트리의 "상태 5종(Idle/Move/Attack/Hit/Dead)"도 현재는 Attack 대신 Casting이다. 이하 본문은 작성 시점의 기록이다.

---

## 1. 개요 & 분석 범위

`PlayerRoot`는 플레이어 오브젝트 그래프의 조립 루트(Composition Root)다. 직전 세션에서 `PlayerRoot` **단일 파일** 리팩터링(ITickable 도입, 조립 단일화, 버그 수정, 디버그 분리)은 완료됐다(→ §6 기준선). 이 문서는 한 단계 위, **PlayerRoot가 조립하고 매 프레임 구동하는 하위 시스템 전체**를 대상으로 구조적 문제를 정리한다.

### 의존 그래프 (텍스트 트리)

```
PlayerRoot (Composition Root, MonoBehaviour)
├─ Stats
│  ├─ PlayerStatComponent ── StatMachine ── StatCatalog / StatMath / StatSnapshot
│  ├─ PlayerStatOrchestrator (base/equip/buff → StatMachine)
│  └─ PlayerProgressionController ── IPlayerBaseStatResolver ── PlayerBaseStatSet
├─ Equipment / Buff
│  ├─ PlayerEquipmentController ── EquipmentDefinition
│  └─ PlayerBuffController ── BuffDefinition
├─ Combat
│  └─ PlayerCombatController ── PlayerStatComponent
├─ Skills
│  ├─ PlayerSkillController ── SkillLoadout / SkillCooldownTracker
│  │   ├─ ISkillEffect ── AttackSkillEffect / BuffSkillEffect ── SkillContext
│  │   └─ ICastGate ── PlayerStateMachineCastGate ── PlayerStateMachine
│  ├─ AutoCastController ── AutoBattleInputSource
│  └─ SkillButton (UI)
├─ StateMachine (별도 MonoBehaviour: PlayerStateMachineDriver)
│  └─ PlayerStateMachine ── PlayerStateContext ── 상태 5종(Idle/Move/Attack/Hit/Dead)
├─ Input / Movement
│  ├─ IMoveInputSource ── JoystickInputReader(능동) / AutoBattleInputSource(방치)
│  └─ IPlayerMovementController ── PlayerMovementController ── PlayerStat(SO)
├─ Enemy / Targeting
│  └─ ITargetProvider ── NearestEnemyTargetProvider ── EnemyRegistry(static) ── EnemyUnit
└─ Presentation
   └─ PlayerHudBinder
```

---

## 2. 현재 아키텍처 요약 (계층별 책임)

| 계층 | 핵심 타입 | 책임 | 평가 |
|---|---|---|---|
| Composition | `PlayerRoot` | 조립·초기화·틱 순회 | 직전 리팩터링으로 양호 |
| Stats | `StatMachine`, `PlayerStatComponent` | base+modifier 계산, 자원(HP/MP) | 계산 구조는 견고(dirty 캐시, 스냅샷) |
| Orchestration | `PlayerStatOrchestrator` | 정의(SO)→모디파이어 변환·적용 | SRP 양호 |
| Skills | `PlayerSkillController` | 시전·쿨다운·마나·이동차단 | 전략(Effect)·CastGate 분리 우수 |
| StateMachine | `PlayerStateMachine` | 상태 전이 | 코어 견고, 상태 구현은 미완 |
| Input/Move | `IMoveInputSource`, `PlayerMovementController` | 입력 추상·물리 이동 | **스탯 소스 이원화 문제(§3-A)** |
| Presentation | `PlayerHudBinder` | 스탯 표시 | 디버그 로그 수준(임시) |

전반적으로 **인터페이스 분리(ISP)와 전략 패턴 적용은 우수**하다. 문제는 대부분 "두 시스템이 같은 개념을 중복 소유"하거나 "핵심 기능(모드 전환·사망)이 아직 배선되지 않은" 데서 온다.

---

## 3. 발견 사항

각 항목: **근거 → 영향 → 제안 → 노력/리스크**. 심각도 순.

### 🔴 심각 (아키텍처 · 정합성)

#### A. 이중 스탯 소스 — Single Source of Truth 위반

- **근거:** 같은 개념(MoveSpeed/AttackSpeed/MaxHp/Crit)이 **두 곳**에 존재한다.
  - 런타임 `StatMachine`: `StatCatalog.cs:16`(MoveSpeed 기본 5), 오케스트레이터가 base/장비/버프를 반영(`PlayerStatOrchestrator.cs:16-17`).
  - SO `PlayerStat`: `Data/Definitions/PlayerStat.cs:8-14`(MaxHp/Attack/AttackSpeed/Crit/MoveSpeed).
  - 실제 이동은 **SO 값**을 읽는다: `PlayerMovementController.cs:79` → `_moveDirection * playerStat.MoveSpeed`.
  - 반면 `StatType.MoveSpeed`의 유일한 소비처는 **HUD 로그**뿐(`PlayerHudBinder.cs:38`). `StatType.AttackSpeed`도 DPS "계산 표시"용(`PlayerStatComponent.cs:70`, `ComputeDps`)일 뿐 실제 공격 주기에 연결되지 않음.
- **영향:** **버프·장비로 올린 이동속도/공격속도가 실제 플레이에 반영되지 않는다.** 성장·버프가 방치형의 핵심인데 수치와 체감이 어긋난다. 두 값을 각각 관리하면 향후 계속 동기화 버그를 낳는다.
- **제안:**
  1. `PlayerMovementController`가 `playerStat.MoveSpeed` 대신 `PlayerStatComponent.Stats.GetFinal(StatType.MoveSpeed)`를 읽도록 전환(주입 필요 → PlayerRoot가 movement에 stat 접근자 전달, 또는 `IReadOnlyStats` 추상 주입).
  2. `PlayerStat` SO는 **전투 수치 필드 제거**, 순수 설정(`InputDeadZone`, `useSpriteFlip`)만 남긴다. 혹은 `MovementSettings`로 개명.
  3. 공격 주기도 추후 `AttackSpeed`를 실제 시전/평타 간격에 연결(F와 연계).
- **노력:** 중 / **리스크:** 중(이동에 stat 의존성 주입 → 배선 변경). 단, 스탯 계산 구조 자체는 이미 견고하므로 소비처만 바꾸면 됨.

#### C. 입력 소스 스왑 메커니즘 부재 — 하이브리드 장르의 핵심 기능

- **근거:** `JoystickInputReader`(능동)와 `AutoBattleInputSource`(방치) 둘 다 `IMoveInputSource`를 구현(추상화는 완료). 그러나 소비처가 **하나의 소스를 SerializeField로 고정**한다:
  - `PlayerMovementController.cs:10` `inputSourceBehaviour`
  - `PlayerStateMachineDriver.cs:7` `joystickInputReader`
- **영향:** 게임 방향([[game-genre-hybrid-idle-combat]])의 핵심인 **방치↔능동 런타임 전환이 구조적으로 불가능**. 두 소비처가 서로 다른 소스를 참조하면 이동과 상태 판정이 어긋날 수도 있다.
- **제안:** "현재 활성 소스"를 감싸 런타임에 교체하는 **입력 라우터**(`IMoveInputSource`를 구현하고 내부에서 active 소스로 위임)를 도입. `PlayerRoot`가 모드를 소유하고 `SetMode(Idle/Active)`로 라우터의 active를 스왑. movement·driver는 라우터 하나만 참조.
- **노력:** 중 / **리스크:** 낮음(신규 어댑터 추가 + 배선 일원화, 기존 소스 구현 불변).

#### F. 플레이어 사망/피격 경로 미연결

- **근거:** `PlayerCombatController.TakeDamage` → `PlayerStatComponent.ApplyDamage`로 HP만 깎는다. HP 0 도달 시 상태머신 `Hit`/`Dead` 전이나 게임오버 처리가 없다. 상태 클래스 `PlayerState_Attack/Idle/Move`는 전이 로직만 있고 `Hit`/`Dead`는 사실상 빈 껍데기. 반면 `EnemyUnit`은 자체 HP·`Die()`를 보유(`EnemyUnit.cs:27`)해 **적/플레이어 피격 처리가 비대칭**.
- **영향:** 능동 전투(보스/레이드)에서 플레이어가 죽지 않는다. 계획서의 `CancelCast`/`NotifyHit`가 붙을 지점이 비어 있음.
- **제안:** 플레이어 자원 컴포넌트가 사망 이벤트(`OnDied`)를 발행 → `PlayerRoot`(또는 전용 어댑터)가 상태머신 `Dead` 전이 및 시전 취소(`ICastGate` 확장 `CancelCast`)를 트리거. 플레이어도 `IDamageable`로 통일해 피격 소스를 일원화.
- **노력:** 중~대 / **리스크:** 중(상태머신·스킬·전투 3자 연동 신규 설계).

### 🟡 중간 (유지보수성 · 재사용)

#### B. MonoBehaviour + `as` 캐스팅 배선 패턴 중복

- **근거:** 동일 패턴이 3곳 반복 — `[SerializeField] MonoBehaviour` + `as IInterface` + null 로그.
  - `PlayerRoot.cs`(movementBehaviour), `PlayerStateMachineDriver.cs:21-30`, `PlayerMovementController.cs:28-34`.
- **영향:** 오배선(GameObject 잘못 드래그 등)이 **런타임에만** 발각. 검증 코드가 파일마다 복붙됨(DRY 위반).
- **제안:** 재사용 헬퍼 도입 — 인스펙터에서 인터페이스 슬롯을 안전하게 받는 `InterfaceReference<T>` 래퍼, 또는 공통 `TryResolve<T>(MonoBehaviour, out T, Object context)` 검증 유틸.
- **노력:** 소~중 / **리스크:** 낮음.

#### E. Presentation이 디버그 로그로 대체됨

- **근거:** `PlayerHudBinder`가 실제 UI 없이 `Debug.Log`만 출력(`:33`), `OnStatChanged`마다 전체 문자열을 로깅(`:42-45`).
- **영향:** 초기화 시 스탯이 연쇄 변경되며 로그가 반복 출력(스팸). 실 UI로 교체할 때 인터페이스 부재로 결합도 큼.
- **제안:** `IPlayerHud` 추상화 도입 → 로그 구현(`DebugHud`)과 실제 UI 구현을 교체 가능하게. 갱신은 K와 함께 스냅샷 기반 1회로.
- **노력:** 소~중 / **리스크:** 낮음.

#### K. 스탯 변경 브로드캐스트 비효율

- **근거:** `StatMachine`에 배치 인프라(`OnSnapshotChanged`, `GetSnapshot`, `ForceRecalculateAll`)가 있으나(`StatMachine.cs:126-134`), `PlayerHudBinder`는 개별 `OnStatChanged`를 구독(`:13`)해 스탯당 리프레시.
- **영향:** 초기화·다중 모디파이어 적용 시 N번 갱신. 실 UI에서 프레임당 여러 번 리빌드 위험.
- **제안:** 소비처를 `OnSnapshotChanged` 기반으로 전환하거나, 프레임 말 1회 dirty flush. 이미 존재하는 인프라 재사용이라 신규 코드 최소.
- **노력:** 소 / **리스크:** 낮음.

#### G. 스킬 편성 하드코딩 (이미 계획됨)

- **근거:** `PlayerRoot.basicAttack` + `equippedSkills[]`가 오브젝트에 직접 박힘.
- **상태:** **이미 로드맵 존재** — 계획서 12장 `SkillLoadoutConfig` SO(L-A 데이터 → L-B PlayerRoot가 config 참조 → L-C 씬 편성). 본 제안서는 재확인만.
- **노력:** 중 / **리스크:** 낮음(계획대로).

### 🟢 낮음 (설계 명료성 · 위생)

#### D. `Attack` 상태 의미 과부하

- **근거:** `PlayerStateMachineCastGate`가 **시전 상태로 `Attack`을 재사용**(`PlayerStateMachineCastGate.cs:9`, 기본값 `Attack`)하는데 `PlayerState_Attack`은 빈 구현. 즉 "평타"와 "스킬 시전"이 같은 상태로 뭉개짐.
- **영향:** 지금은 무해하나, 시전 중 애니메이션/피격 처리를 붙일 때 평타와 구분이 어려워짐.
- **제안:** 시전 전용 상태(`Casting`) 분리 검토, 또는 CastGate 생성 시 명시적 상태 매핑 + 의도 주석.
- **노력:** 소 / **리스크:** 낮음.

#### H. 정적 전역 `EnemyRegistry`

- **근거:** `static List<EnemyUnit>`(`EnemyRegistry.cs:5`). `EnemyUnit`이 `OnEnable/OnDisable`로 등록/해제해 실사용은 무난.
- **영향:** 테스트 격리 어려움, 멀티씬/도메인 리로드 경계에서 잔존 위험(낮음).
- **제안:** 장기적으로 주입 가능한 타겟팅 서비스로. **현재 우선순위 낮음** — 기록만.
- **노력:** 중 / **리스크:** 중(광범위 참조 변경) → 후순위.

#### I. `PlayerSkillController`의 구체 타입 의존

- **근거:** `PlayerCombatController`/`PlayerStatComponent`/`PlayerBuffController` 구체 클래스 직접 의존(`PlayerSkillController.cs:8-10`). 단, `ICastGate`·`IPlayerMovementController`는 이미 추상화되어 양호.
- **영향:** 단위 테스트 시 실제 컨트롤러 필요. DIP 관점 개선 여지.
- **제안:** 필요한 최소 능력만 인터페이스로(`IDamageDealer`, `IResourceSpender` 등). **단 과설계 주의** — 테스트/교체 요구가 실제로 생길 때 도입. 지금은 "인지"만.
- **노력:** 중 / **리스크:** 낮음(선택적).

#### J. 네이밍 · 오타 위생

- 파일명 `Features/Enemy/NearesEnemyTargetProvider.cs` — 클래스는 `NearestEnemyTargetProvider`(파일명 오타).
- `SkillDefinition.cs:23` Header `"Csting Behavior"` → `Casting`.
- `PlayerStat.cs:7` Header `"Movement Settings"`인데 실제로는 전투 스탯 포함(A 해결 시 자연 정리).
- `StatMachine.cs:37,67` `// 재공부` 개발 메모 잔존.
- **노력:** 소 / **리스크:** 낮음(단 파일/필드명 변경 시 §5 배선 주의).

---

## 4. 권장 실행 순서 (로드맵)

우선순위는 **게임 플레이에 실제 영향을 주는 정합성**을 앞세운다.

| 단계 | 항목 | 이유 |
|---|---|---|
| 1 | **A. 스탯 소스 단일화** | 성장·버프가 체감에 반영되게 하는 정합성의 근본. 이후 F의 공격속도 연결도 여기 의존. |
| 2 | **C. 입력 라우터** | 하이브리드 장르의 핵심 기능(모드 전환) 활성화. 신규 어댑터라 리스크 낮음. |
| 3 | **F. 사망/피격 파이프라인** | 능동 전투 성립 조건. A(공격속도)·상태머신과 연동. |
| 4 | **B / E / K** | 배선 통일 + Presentation 추상화 + 갱신 효율. 상호 보완적, 함께 처리 효율적. |
| 5 | **G** | 계획된 `SkillLoadoutConfig` — 별도 챕터로 진행. |
| 6 | **D / I / J / H** | 위생·명료성. 다른 작업에 곁들여 정리. |

> 참고: 각 단계는 독립 검증 가능하도록 **동작 보존(behavior-preserving)** 리팩터링부터, 그다음 기능 추가 순으로 쪼갤 것을 권장.

---

## 5. 리스크 & 씬/프리팹 배선 주의

- **SerializeField 필드/클래스명 변경 시 씬·프리팹 연결이 끊긴다.** 이름 변경이 불가피하면 `[FormerlySerializedAs]` 병행([[combat-skill-plan-progress]] 경고).
- `MonoBehaviour` 인터페이스 슬롯은 인스펙터 ⊙ 버튼으로 **컴포넌트를 직접 지정**(GameObject 드래그 시 오할당 위험).
- 플레이 모드 중 인스펙터 수정은 저장되지 않음.
- A(이동이 stat 참조) 적용 시 `PlayerMovementController`에 stat 접근자 주입 경로가 생기므로, 씬에서 참조 배선 1건 추가 필요.

---

## 6. 기준선 — 이미 완료된 것

직전 세션 `PlayerRoot` 단일 파일 리팩터링(플레이 검증 완료):

- 버그2 수정(ComposeSkills null 검증 선행으로 NRE 제거, 미할당 `Movement` 프로퍼티 삭제).
- 조립 **Awake+Start 분할 → Start 단일 경로**(`Compose→Initialize→RegisterTickables`) 통합.
- **`ITickable` 규약** 도입 — 프레임 갱신 시스템은 이 인터페이스 구현 후 `RegisterTickables()`에 등록(OCP). 현재 stat→buff→skill→autoCast 순.
- 디버그 명령을 `PlayerDebugCommands`(`#if UNITY_EDITOR`)로 분리, 미사용 public 프로퍼티 7개 제거.

본 제안서의 항목들은 이 기준선 위에서 **한 계층 아래(하위 시스템)**를 다룬다.

---

## 7. 구현 반영 현황 (2026-07-09)

로드맵 1~6단계 전체를 구현 반영했다. (컴파일 검증 완료 / 플레이 런타임 검증은 미실시 — §7 하단 주의)

| 항목 | 상태 | 핵심 반영 |
|---|---|---|
| **A. 스탯 소스 단일화** | ✅ | `IReadOnlyStats` 도입, `StatMachine` 구현. `PlayerMovementController`가 `GetFinal(MoveSpeed)`를 읽음. `PlayerStat` SO는 이동/입력 설정만 유지. progression `StartMoveSpeed`=10으로 동작 보존. |
| **C. 입력 라우터** | ⚠️ 코드 완료 | `PlayerInputRouter`(`IMoveInputSource` 위임) + `PlayerControlMode`. `PlayerRoot`가 모드 소유·`SetControlMode`. 이동에 `IMoveInputConsumer`로 주입. **씬 배선 1건 필요**(아래). |
| **F. 사망/피격** | ✅ | `PlayerStatComponent.OnDied`/`IsDead`, `PlayerDeathHandler`(시전 취소→이동 정지→Dead 전이), `PlayerSkillController.CancelCast`, `PlayerState_Dead` 구현, `PlayerCombatController : IDamageable`. |
| **B. 배선 헬퍼** | ✅ | `SerializedInterface.TryResolve<T>` 도입, Driver·PlayerRoot 검증 통일. |
| **E. HUD 추상화** | ✅ | `IPlayerHud` + `DebugPlayerHud` + `PlayerHudSnapshot`. `PlayerHudBinder`는 어댑터로 위임. |
| **K. 갱신 효율** | ✅ | `PlayerHudBinder`가 스탯 변경을 프레임당 1회로 합침(dirty flush). |
| **D. 시전 상태 명료화** | ✅ | `PlayerStateMachineCastGate` 파라미터명·의도 주석 명시(Attack 재사용). 전용 `Casting` 상태는 후속. |
| **J. 위생** | ✅ | `NearestEnemyTargetProvider` 파일명 정정, `SkillDefinition` 헤더 오타, `StatMachine` 개발메모 제거. |
| **G / I / H** | ⏸️ | G는 별도 챕터(계획대로). I·H는 "인지만" — 코드 변경 없음. |

### ⚠️ 남은 수동 작업 (Inspector 1건)

- **C 활성화:** Player 프리팹의 `PlayerRoot.Active Input Source` 슬롯에 조이스틱 입력 컴포넌트(`JoystickInputReader`)를 지정해야 방치↔능동 스왑이 실제로 작동한다. 미지정 시 라우터가 구성되지 않고 이동은 기존 직렬화 소스를 그대로 사용하므로 **동작은 보존**된다.

### 검증 메모

- 컴파일: Unity(Idle Game 인스턴스) 에러 0.
- 런타임(플레이 모드) 체감 검증은 미실시. 특히 (1) 스탯 기반 이동속도 변동, (2) 모드 스왑, (3) HP 0 시 Dead 전이·시전 취소는 플레이로 확인 권장.
  에디터 디버그: `PlayerDebugCommands`의 ContextMenu(`Apply Test Damage` 반복 → 사망, `Toggle Control Mode` → 모드 전환).
