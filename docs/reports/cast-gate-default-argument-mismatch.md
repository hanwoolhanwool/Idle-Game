# 리팩터링 제안 — CastGate 기본 인자 불일치와 `Attack` 상태 정리

> 작성일: 2026-07-10
> 대상: `PlayerStateMachineCastGate`, `PlayerStateID.Attack`, `PlayerState_Attack`
> 성격: **분석·제안 문서** (작성 시점 코드 변경 없음)
> 관련 명세: [skills.md §11](../specs/player/skills.md) · [state-machine.md §11](../specs/player/state-machine.md)

> **후기(2026-07-22) — 구현 완료.** 채택안은 **A1(기본값 제거·필수 인자화) + B1(`Attack` 상태 제거)**. 생성자에서 두 기본 인자를 제거해 호출부가 시전/복귀 상태를 반드시 명시하게 했고, `PlayerStateID.Attack`·`PlayerState_Attack`·Driver 등록을 삭제했다. B2(평타 전용 상태)를 접은 근거: 평타는 슬롯 0의 스킬로 시전 파이프라인을 공유하므로 별도 상태의 실익이 없고, GDD도 평타/스킬을 상태로 구분하지 않는다 — 필요해지면 명시적 전이와 함께 신설한다(YAGNI). `PlayerStateID`는 어디에도 직렬화되지 않아 enum 항목 제거에 따른 씬/프리팹 드리프트는 없다. 이하 본문은 구현 전 진단의 기록이다.

---

## 1. 문제 요약

스킬 시전 상태를 상태머신에 위임하는 `PlayerStateMachineCastGate`의 **생성자 기본값이 `PlayerStateID.Attack`** 인데, 실제 유일한 호출자(`PlayerRoot`)는 **`PlayerStateID.Casting`을 명시 주입**한다. 두 값이 어긋나 있고, 기본값이 가리키는 `Attack` 상태는 **아무도 전이시키지 않는 미사용 상태**다. 지금은 무해하지만, 기본값 경로로 게이트를 생성하는 순간 "시전"이 엉뚱한 상태로 새는 잠복 버그다.

## 2. 근거 (코드)

**게이트 생성자 — 기본값이 `Attack`:**

```csharp
// PlayerStateMachineCastGate.cs
public PlayerStateMachineCastGate(
    PlayerStateMachine stateMachine,
    PlayerStateID castStateID   = PlayerStateID.Attack,  // ← 기본값 Attack
    PlayerStateID returnStateID = PlayerStateID.Idle)
```

**유일한 호출자 — `Casting`을 명시 주입:**

```csharp
// PlayerRoot.cs ComposeSkills()
var castGate = new PlayerStateMachineCastGate(
    stateMachineDriver.StateMachine,
    castStateID:  PlayerStateID.Casting,   // ← 기본값을 덮어씀
    returnStateID: PlayerStateID.Idle);
```

**`Attack` 상태는 미사용:** `TryChangeState(PlayerStateID.Attack)` 호출처가 코드 전체에 **없다**. `PlayerState_Attack`은 `Enter/Exit/FixedTick`이 모두 빈 구현이다. `PlayerStateID` enum에는 `Attack`과 `Casting`이 모두 존재한다.

> 배경: 과거에는 시전 상태로 `Attack`을 재사용했고(구 제안서 §3-D "Attack 상태 의미 과부하"), 이후 전용 `Casting` 상태를 도입하며 호출부는 옮겼으나 **생성자 기본값이 남았다.**

## 3. 영향

| 관점 | 영향 |
|------|------|
| **정합성** | 기본값 경로로 게이트를 생성하면 시전이 `Casting`이 아닌 `Attack`으로 전이 → `IsCasting`(현재 상태==castStateID) 판정이 `Attack` 기준이 되어 시전 잠금·복귀가 어긋남 |
| **가독성** | 생성자 시그니처가 "시전=Attack"이라고 잘못 문서화. 신규 호출자가 기본값을 신뢰하면 버그 |
| **위생** | `Attack` 상태·enum이 "쓰이는 것처럼" 남아 있어 상태 다이어그램·리뷰에 노이즈 |

현재는 유일 호출자가 명시 주입하므로 **런타임 버그로 발현되지 않는다.** 순수 잠복 리스크·명료성 문제다.

## 4. 리팩터링 방안

두 결정을 함께 내려야 한다: **(A) 기본 인자 처리** + **(B) `Attack` 상태의 운명**.

### 4-A. 기본 인자 — 세 가지 선택지

| 안 | 변경 | 장점 | 단점 |
|----|------|------|------|
| **A1 (권장)** | 기본값 제거 → `castStateID`를 **필수 인자**로 | 호출자가 시전 상태를 반드시 의식. 잠복값 소멸 | 호출부(현재 1곳) 시그니처 갱신 |
| A2 | 기본값을 `Attack` → `Casting`으로 변경 | 최소 변경 | "기본 시전 상태"라는 암묵 가정이 남음 |
| A3 | 현행 유지 + 주석만 | 변경 0 | 잠복 리스크 존치 |

**A1 권장.** 시전 상태는 게임마다 다를 수 있는 정책값이라 기본값을 두는 것 자체가 부적절하다. 호출자가 1곳뿐이라 비용도 최소.

```csharp
// A1: 기본값 제거
public PlayerStateMachineCastGate(
    PlayerStateMachine stateMachine,
    PlayerStateID castStateID,
    PlayerStateID returnStateID)   // 기본값 없음 → 호출자가 명시
```

`returnStateID`도 `Idle` 기본값을 유지할지 검토하되, 대칭성을 위해 함께 필수화하는 편이 명료하다.

### 4-B. `Attack` 상태의 운명 — 두 갈래

| 안 | 내용 | 적합 상황 |
|----|------|-----------|
| **B1. 제거** | `PlayerStateID.Attack`·`PlayerState_Attack`·Driver 등록 삭제 | 평타를 `Casting`으로 계속 처리할 계획이면 |
| **B2. 평타 전용으로 활용** | 평타(슬롯 0)는 `Attack`, 스킬(1~5)은 `Casting`으로 분리 전이 | 평타 애니메이션·피격 처리를 스킬과 구분하려면 |

> **판단 기준**: "평타와 스킬 시전을 시각적/규칙적으로 구분할 것인가?"가 기획 질문이다([[skills.md]]의 `SkillType.Attack` vs `Buff`와는 별개 축). 구분 계획이 **없으면 B1(제거)**, 있으면 B2로 `PlayerSkillController`가 슬롯 종류에 따라 다른 `castStateID`를 쓰도록 게이트를 2개 두거나 시전 시점에 상태를 선택.

B2를 택하면 §4-A의 "시전 상태는 정책값" 논거가 더 강해지므로 **A1(필수 인자)과 자연스럽게 맞물린다.**

## 5. 노력 / 리스크

| 항목 | 평가 |
|------|------|
| 노력 | **소** — A1은 시그니처 1개 + 호출부 1곳. B1은 파일 1개 + enum 1줄 + Driver 등록 1줄 삭제 |
| 리스크 | **낮음** — 동작 보존 리팩터링. B1 삭제 시 `Attack`을 참조하는 곳이 없음을 grep으로 재확인 |
| 씬/프리팹 영향 | 없음(상태는 코드 등록, 직렬화 필드 아님) |

## 6. 검증 방법

- **컴파일**: `Attack` 참조 잔존 여부 컴파일 에러로 확인.
- **grep**: `PlayerStateID.Attack` / `PlayerState_Attack` 잔존 0건 확인(B1 선택 시).
- **런타임**: 스킬 시전 → `IsCasting==true` + 상태가 `Casting`, 종료 후 `Idle` 복귀. 시전 중 재시전 차단 확인([[skills.md]] §10 테스트).
- **EditMode**: 목 상태머신으로 `EnterCast`/`ExitCast`가 주입한 상태 ID로 전이하는지 단언.

## 7. 권장 결론

1. **A1** — `PlayerStateMachineCastGate` 생성자에서 `castStateID` 기본값 제거(필수 인자화).
2. **B**는 기획 확정 후: 평타/스킬 구분 계획이 없으면 **B1(Attack 제거)**, 있으면 **B2** + 게이트 상태 선택 로직.
3. 두 작업은 §[[state-machine.md]] §11의 "`Attack` 상태 미사용" TBD를 함께 해소한다.
