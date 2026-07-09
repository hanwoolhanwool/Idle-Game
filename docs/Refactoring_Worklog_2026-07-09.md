# PlayerRoot 하위 시스템 리팩터링 — 작업 로그

> 작업일: 2026-07-09
> 근거 문서: [`PlayerRoot_Refactoring_Proposal.md`](./PlayerRoot_Refactoring_Proposal.md)
> 범위: 제안서 로드맵 1~6단계 구현 + 입력 라우터 씬 배선
> 브랜치: `main` (원격 `origin/main` 푸시 완료, 단 씬 배선 커밋은 별도 확인 대기)

---

## 1. 개요

제안서에서 정리한 `PlayerRoot` 하위 시스템의 구조·정합성 문제(A~K)를 구현으로 해소했다.
핵심 축은 **"두 시스템이 같은 개념을 중복 소유"하던 것을 단일 출처로 모으고, 아직 배선되지 않았던
핵심 기능(모드 전환·사망)을 실제로 연결"**하는 것이다.

- 컴파일: Unity(Idle Game) 에러 0
- 런타임(플레이 모드) 체감 검증: **미실시** (권장 항목은 §6 참고)

---

## 2. 커밋 요약

| 커밋 | 타입/스코프 | 내용 | 규모 |
|---|---|---|---|
| `dc3c42e` | refactor(player) | A 스탯 SoT · C 입력 라우터 · F 사망 파이프라인 · B 배선 헬퍼 · D 시전 상태 | 27파일, +363/−52 |
| `5ea8df7` | refactor(player-hud) | E HUD 추상화 · K 갱신 합치기 | 7파일, +118/−17 |
| `2be0845` | chore(hygiene) | J 파일명·헤더 오타 | 3파일 |
| `b2db818` | docs(player) | 리팩터링 제안서 문서 | 1파일, +223 |
| *(미커밋)* | chore(scene) | C 활성화 씬 배선(`activeInputSource`) + HUD 필드 재직렬화 | `SampleScene.unity` |

> `PlayerRoot`가 A·C·F·B·D 다섯 관심사에 걸쳐 있어 파일 단위로는 그보다 잘게 못 나눠, 첫 커밋에 함께 담고 본문에 항목별로 구분 기재했다.

---

## 3. 항목별 상세

### A. 이중 스탯 소스 → 단일 출처(SoT)
- **문제:** 같은 개념(MoveSpeed 등)이 런타임 `StatMachine`과 SO `PlayerStat` 양쪽에 존재하고, 실제 이동은 SO 값만 읽어 버프·장비가 이동에 반영되지 않았다.
- **변경:**
  - `IReadOnlyStats`(신규) 도입 → `StatMachine`이 구현. 소비처는 최소 계약에만 의존(ISP/DIP).
  - `PlayerMovementController`가 `playerStat.MoveSpeed` 대신 `IReadOnlyStats.GetFinal(MoveSpeed)`를 읽음. 스탯은 조립 루트가 `IStatDrivenMovement.BindStats`로 주입.
  - `PlayerStat` SO에서 전투 스탯 필드 제거(이동/입력 설정만 유지).
  - **동작 보존:** 실제 이동 속도가 SO(10)와 progression(5)로 어긋나 있어, `PlayerProgressionConfig.StartMoveSpeed`를 10으로 맞춤.

### C. 입력 라우터 (방치↔능동 런타임 전환)
- **문제:** 하이브리드 장르의 핵심인 모드 전환이 구조적으로 불가능(소비처가 단일 소스를 고정 참조).
- **변경:**
  - `PlayerControlMode`(enum: Active/Idle), `PlayerInputRouter`(`IMoveInputSource` 위임) 신규.
  - `PlayerRoot`가 모드를 소유하고 `SetControlMode`/`ControlMode` 노출. `IMoveInputConsumer`로 이동에 라우터 주입.
  - 상태들은 `PlayerMovementController.MoveInput`만 읽으므로, 라우터를 movement 하나에만 주입하면 상태 판정까지 투명하게 동작(driver의 `MoveInputSource`는 미사용 죽은 배선임을 확인).
  - **미배선 시 폴백:** `activeInputSource` 미지정이면 라우터를 만들지 않아 기존 동작 보존.
- **씬 배선(§4):** `SampleScene`의 `PlayerRoot.activeInputSource`를 `JoystickInputReader`에 연결해 기능 활성화.

### F. 사망/피격 파이프라인
- **문제:** HP 0 도달 시 상태머신 `Dead` 전이·시전 취소가 없어 능동 전투가 성립하지 않음(적/플레이어 피격 처리 비대칭).
- **변경:**
  - `PlayerStatComponent`에 `OnDied` 이벤트 + `IsDead` 추가(HP 0 도달 시 1회 발행, 사망 후 데미지 무시).
  - `PlayerDeathHandler`(신규 어댑터): `OnDied` 구독 → 시전 취소 → 이동 정지 → `Dead` 전이(SRP로 `PlayerRoot` 비대화 방지).
  - `PlayerSkillController.CancelCast()` 추가, `TryUseSkill`에 `IsDead` 가드.
  - `PlayerState_Dead` 구현(진입 시 이동 정지 방어).
  - `PlayerCombatController : IDamageable`로 통일(`TakeDamage` → `ApplyDamage`).
- **미완:** `Hit`(경직) 상태와 적→플레이어 실제 공격 연결은 후속(§6).

### B. 배선 헬퍼 (DRY)
- **문제:** `[SerializeField] MonoBehaviour` + `as IInterface` + null 로그 패턴이 3곳 복붙.
- **변경:** `SerializedInterface.TryResolve<T>`(신규) 도입 → `PlayerStateMachineDriver`, `PlayerRoot`의 검증 통일.

### E. Presentation 추상화
- **문제:** `PlayerHudBinder`가 실제 UI 없이 `Debug.Log`만 출력, 교체 어려움.
- **변경:** `IPlayerHud` + `DebugPlayerHud` + `PlayerHudSnapshot`(DTO) 신규. `PlayerHudBinder`는 어댑터로 위임(로그↔실 UI 교체 가능).

### K. 스탯 변경 브로드캐스트 효율화
- **문제:** 스탯당 개별 갱신으로 초기화·다중 모디파이어 시 N회 리프레시.
- **변경:** `PlayerHudBinder`가 변경을 dirty flag로 모아 **프레임당 1회**만 렌더.

### D. 시전 상태 의미 명료화
- **변경:** `PlayerStateMachineCastGate` 파라미터명(`castStateID`)·의도 주석 명시. `PlayerRoot`가 매핑을 명시적으로 주입(`Attack` 재사용). 전용 `Casting` 상태 분리는 후속.

### J. 네이밍·위생
- `NearesEnemyTargetProvider.cs` → `NearestEnemyTargetProvider.cs` (파일명 정정, 클래스명과 일치).
- `SkillDefinition` 헤더 `"Csting Behavior"` → `"Casting Behavior"`.
- `StatMachine`의 개발 메모(`// 재공부`) 제거.

### I / H (의도적 보류)
- **I** `PlayerSkillController` 구체 타입 의존: 과설계 방지 위해 "인지만", 코드 변경 없음.
- **H** static `EnemyRegistry`: "기록만", 변경 없음.

---

## 4. 씬 배선 (C 활성화)

`Assets/Dev/SampleScene.unity`의 Player(PlayerRoot)에 대해:

- `activeInputSource` → **JoystickInputReader** (프리팹 상속 컴포넌트를 stripped stub으로 정상 참조)
- `autoBattle` → AutoBattleInputSource (기존)
- `initialControlMode` → **Active(0)**

> 참고: 이 씬은 이전엔 입력을 전부 AutoBattle로 몰아 **기본이 자동전투**였으나, 라우터 활성화 + `initialControlMode=Active`로 **기본이 조이스틱 조작**으로 바뀐다. 방치형 기본을 원하면 `initialControlMode`를 `Idle`로 조정.

---

## 5. 파일 변경 목록

### 신규 (10)
| 파일 | 역할 |
|---|---|
| `Stats/Core/IReadOnlyStats.cs` | 스탯 최종값 읽기 전용 추상 |
| `Movement/IStatDrivenMovement.cs` | 이동에 스탯 주입 계약 |
| `Movement/IMoveInputConsumer.cs` | 이동에 입력 소스 주입 계약 |
| `Input/PlayerControlMode.cs` | 제어 모드 enum |
| `Input/PlayerInputRouter.cs` | 입력 소스 라우터 |
| `Combat/PlayerDeathHandler.cs` | 사망 연동 어댑터 |
| `Shared/Utils/SerializedInterface.cs` | 인터페이스 슬롯 해석 헬퍼 |
| `Presentation/IPlayerHud.cs` | HUD 추상 |
| `Presentation/DebugPlayerHud.cs` | 로그 HUD 구현 |
| `Presentation/PlayerHudSnapshot.cs` | HUD 표시 DTO |

### 리네임 (1)
- `Enemy/NearesEnemyTargetProvider.cs` → `Enemy/NearestEnemyTargetProvider.cs`

### 수정 (15)
- `Composition/PlayerRoot.cs`
- `Movement/PlayerMovementController.cs`
- `Stats/Runtime/PlayerStatComponent.cs`
- `Stats/Core/StatMachine.cs`
- `Combat/PlayerCombatController.cs`
- `Skills/PlayerSkillController.cs`
- `Skills/Adapters/PlayerStateMachineCastGate.cs`
- `StateMachine/Core/PlayerStateMachineDriver.cs`
- `StateMachine/States/PlayerState_Dead.cs`
- `Presentation/PlayerHudBinder.cs`
- `Presentation/PlayerDebugCommands.cs`
- `Data/Definitions/PlayerStat.cs`
- `Data/Definitions/SkillDefinition.cs`
- `Data/Player/PlayerProgressionConfig.asset`
- `Dev/SampleScene.unity`

---

## 6. 남은 작업 (후속)

| 항목 | 내용 |
|---|---|
| **F 마저** | `Hit`(경직) 상태 구현 + 적→플레이어 공격 연결(플레이어를 실제로 때리는 소스) |
| **G** | `SkillLoadoutConfig` SO로 스킬 편성 하드코딩 제거(별도 챕터) |
| **D 심화** | 전용 `Casting` 상태 분리 |
| **위생(프리팹)** | 프리팹 `PlayerMovementController` 필드 리네임 드리프트(`inputReader`→`inputSourceBehaviour`)로 끊긴 조이스틱 링크 정리 |
| **커밋** | 씬 배선(`SampleScene.unity`) 커밋 |

### 런타임 검증 권장 시나리오
1. 스탯 기반 이동속도 — 이동 버프 적용 시 실제 속도 변화(`Apply First Start Buff`).
2. 모드 전환 — `Toggle Control Mode (Active/Idle)`로 조이스틱↔자동전투 스왑.
3. 사망 — `Apply Test Damage` 반복 → HP 0 → `Dead` 전이·시전/이동 정지.

---

## 7. 참고 — 검증·복구 이력

- MCP 인스턴스 라우팅이 다중 연결 시 다른 프로젝트(Mecha Survivor)를 가리키는 문제가 있어, 씬/프리팹 쓰기는 다른 인스턴스를 닫고 Idle Game 단독 연결을 확인한 뒤 수행.
- 다른 분기의 프로젝트 사고 이후 작업트리 무결성 점검 완료(변경 파일 목록·내용 마커·잔재 파일·브랜치/HEAD/stash 모두 정상).
