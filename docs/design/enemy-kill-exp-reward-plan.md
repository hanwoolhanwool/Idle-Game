# 경험치 공급원(적 처치 보상) 구현 계획서

> **종류**: 설계 명세 (TDD) · **상태**: Draft
> **최종 갱신**: 2026-07-11 · **관련 기획서**: (링크 예정)
> **관련 명세**: [progression.md](../specs/player/progression.md) · [combat.md](../specs/player/combat.md)

---

## 0. 이 계획서의 출발점

방치형 핵심 루프(적 처치 → 경험치 → 레벨업 → 강해짐)를 코드와 대조한 결과, **경험치를 주는 주체가 없음**을 확인했다. `PlayerProgressionController.AddExp`는 존재하지만 이를 호출하는 곳은 에디터 디버그 훅(`PlayerRoot.DebugGainExp`)뿐이고, [[progression]] §2가 "경험치를 **주는** 주체"를 명시적으로 out of scope로 두었다. 몬스터(`EnemyUnit.Die`)는 처치돼도 경험치 개념이 없다.

이 계획서는 **그 첫 링크를 잇는다**: 몬스터를 잡으면 플레이어에게 경험치가 들어온다. 단, [[combat]]이 세운 **"적은 플레이어를 몰라야 한다(DIP)"** 원칙을 경험치 방향에도 그대로 적용한다.

---

## 1. 개요·목적

적 처치 시 플레이어가 경험치를 획득하는 **보상 배선**이다. 적(`EnemyUnit`)은 처치 보상을 **발행만** 하고 누가 받는지 모른다. 순수 C# 브리지가 이를 구독해 성장 시스템의 경험치 수신 계약(`IExpReceiver`)으로 전달한다.

핵심 판단은 **"보상 발행"과 "경험치 수신"의 결합 분리**다. 적 도메인과 성장 도메인이 서로를 직접 참조하면 [[combat]]의 `IDamageable` DIP 원칙이 깨진다. 그래서 그 사이에 정적 이벤트 허브(`EnemyKillReward`)와 브리지 어댑터(`EnemyExpRewardHandler`)를 두어, 두 도메인의 상호 무지를 유지한다.

## 2. 범위(Scope)

| 구분 | 내용 |
|------|------|
| **포함** | 처치 보상 이벤트 허브(`EnemyKillReward`), 경험치 수신 계약(`IExpReceiver`), 브리지 어댑터(`EnemyExpRewardHandler`), `EnemyUnit`에 `expReward` 필드 + `Die()` 발행, `PlayerProgressionController`의 `IExpReceiver` 구현, `PlayerRoot` 배선(생성·Dispose) |
| **미포함(Out of scope)** | 경험치 **획득량 밸런싱**(적별 수치는 데이터 조정 영역), `EnemyStat` SO 통합(현재 `EnemyUnit`이 미사용 — §12), 경험치 획득 배율(`ExpGainRate` 스탯 연동 — [[progression]] §12 확장 여지), 세이브/로드(진행 영속성 — 별도 과제), **레벨→스탯 리졸버**(레벨업이 스탯에 반영되게 하는 다음 과제 — §11) |

## 3. 요구사항·설계 목표 (요구사항 → 설계 해석)

| 요구사항 | 설계적 해석 |
|----------|-------------|
| 몬스터 처치 시 경험치 획득 | `EnemyUnit.Die()`가 처치 보상을 발행 → 브리지가 `AddExp`로 전달 |
| 적이 플레이어를 몰라야 함(DIP) | 적은 `EnemyKillReward.Publish(exp)`로 **발행만**. 수신 측을 참조하지 않음 |
| 성장이 적을 몰라야 함 | `PlayerProgressionController`는 `IExpReceiver`만 구현. 허브를 직접 구독하지 않음 |
| 진짜 사망에서만 지급(풀링 오지급 방지) | 발행을 `Die()` 안, `SetActive(false)` **전**에 둠. `OnDisable`(despawn) 경로와 분리 |
| 지급량 0/음수 방어 | 허브 `Publish`가 0 이하를 무시 |
| 이벤트 구독 누수 방지 | 브리지를 `IDisposable`로, `PlayerRoot.OnDestroy`가 해제 |

## 4. 시스템 구조

```
Enemy 도메인      EnemyUnit.Die() ──publish──> EnemyKillReward (정적 이벤트 허브)
                                                     │ (Rewarded 이벤트)
                                                     ▼
경계(브리지)                              EnemyExpRewardHandler  [IDisposable]
                                                     │ (IExpReceiver.AddExp)
                                                     ▼
Player 성장 도메인                        PlayerProgressionController : IExpReceiver
```

```mermaid
classDiagram
    class EnemyUnit {
        -int expReward
        -Die()
    }
    class EnemyKillReward {
        <<static>>
        +event Rewarded
        +Publish(int)
    }
    class IExpReceiver {
        <<interface>>
        +AddExp(int)
    }
    class EnemyExpRewardHandler {
        <<IDisposable>>
        +Dispose()
    }
    class PlayerProgressionController {
        +AddExp(int)
    }

    EnemyUnit ..> EnemyKillReward : Publish
    EnemyExpRewardHandler ..> EnemyKillReward : subscribe
    EnemyExpRewardHandler --> IExpReceiver : forward
    IExpReceiver <|.. PlayerProgressionController
```

> 적과 성장은 서로를 참조하지 않는다. `EnemyExpRewardHandler`만 양쪽을 안다 — 이 브리지가 유일한 결합점이다.

## 5. 데이터 구조

이 시스템은 **신규 ScriptableObject를 만들지 않는다.** 지급량만 데이터로 노출한다.

| 데이터 | 위치 | 의미 |
|--------|------|------|
| `expReward` | `EnemyUnit`의 `[SerializeField] int` (기본 10) | 이 적을 처치할 때 지급할 경험치 |

> **왜 `EnemyStat` SO가 아니라 필드인가**: 현재 `EnemyUnit`은 `EnemyStat`을 전혀 사용하지 않고 `maxHp`도 자체 serialized 필드로 들고 있다. 이 계획은 경험치 배선에 집중하고, 적 능력치를 SO로 모으는 일은 별도 과제로 남긴다(§12). 지금은 `maxHp`와 **동일한 방식**으로 `expReward`를 두어 일관성을 지킨다.

## 6. 상세 로직·상태

### 6.1 처치 → 경험치 전체 흐름

```mermaid
sequenceDiagram
    participant E as EnemyUnit
    participant Hub as EnemyKillReward
    participant Br as EnemyExpRewardHandler
    participant Prog as PlayerProgressionController

    E->>E: ApplyDamage → HP <= 0
    E->>E: Die()
    E->>Hub: Publish(expReward)
    Note over Hub: expReward <= 0 이면 무시
    Hub-->>Br: Rewarded(exp)
    Br->>Prog: AddExp(exp)
    E->>E: gameObject.SetActive(false)
```

### 6.2 발행 시점 (풀링 오지급 방지)

```mermaid
flowchart TD
    A["Die()"] --> B["currentHp = 0"]
    B --> C["EnemyKillReward.Publish(expReward)"]
    C --> D["gameObject.SetActive(false)"]
    D --> E["OnDisable → EnemyRegistry.UnRegister"]
```

- **발행은 `SetActive(false)` 전에** 한다. `OnDisable`은 풀링 despawn·씬 언로드에서도 불리므로 경험치 훅으로 쓰면 오지급이 생긴다. "실제 사망" 경로인 `Die()`에서만 발행해 이를 분리한다.

### 6.3 허브 발행·구독 (`EnemyKillReward`)

- `Publish(int exp)`: `exp <= 0`이면 무시(가드), 아니면 `Rewarded` 발행.
- `Rewarded`는 `Action<int>` 정적 이벤트. 브리지가 구독/해제한다.
- 다중 처치는 발행마다 독립적으로 전달돼 **누적**된다(허브는 상태를 갖지 않음).

### 6.4 브리지 수명 (`EnemyExpRewardHandler`)

```mermaid
stateDiagram-v2
    [*] --> Subscribed: ctor(IExpReceiver) → Rewarded += Handle
    Subscribed --> Subscribed: Rewarded(exp) → receiver.AddExp(exp)
    Subscribed --> [*]: Dispose() → Rewarded -= Handle
```

- 생성자에서 `IExpReceiver`를 주입받고 허브를 구독한다(null 방어).
- `Dispose()` 후에는 발행이 와도 전달하지 않는다(구독 해제 → 누수 차단).

## 7. 인터페이스·의존성(경계)

| 계약 | 방향 | 설명 |
|------|------|------|
| `IExpReceiver.AddExp(int)` | 브리지가 **호출** | 경험치 수신 진입점. `PlayerProgressionController`가 구현(기존 `AddExp` 재사용) |
| `EnemyKillReward.Publish(int)` | `EnemyUnit`이 **호출** | 처치 보상 발행. 수신 측을 모름 |
| `EnemyKillReward.Rewarded` | 브리지가 **구독** | 보상 이벤트. `EnemyExpRewardHandler`가 `AddExp`로 포워딩 |
| `EnemyExpRewardHandler(IExpReceiver)` | `PlayerRoot`가 **생성·Dispose** | 브리지 수명 관리([[combat]]의 `PlayerDeathHandler`와 동일 패턴) |

> **경계 원칙**: 적↔성장 직접 참조 금지. 모든 전달은 허브+브리지를 통과한다. 이는 [[combat]]이 `IDamageable` 뒤로 피격 소스를 숨긴 것과 같은 결이다. `IExpReceiver`(경계)를 **먼저 못박아** 적 도메인과 성장 도메인의 작업을 병렬화할 수 있다.

## 8. 설계 포인트 (SOLID 매핑)

| 원칙 | 적용 |
|------|------|
| **SRP** | 발행(허브)·전달(브리지)·수신(컨트롤러)이 각각 한 책임 |
| **OCP** | 처치 보상에 새 반응(킬 카운트·퀘스트 등)을 추가하려면 허브에 구독자만 더한다. 기존 코드 불변 |
| **LSP** | `IExpReceiver`를 목으로 대체해 브리지를 단독 검증(EditMode) |
| **ISP** | `IExpReceiver`는 `AddExp` 하나만 — 보상 지급 측은 그 이상 알 필요 없음 |
| **DIP** | 적이 구체 컨트롤러가 아닌 이벤트/추상에 의존. 적·플레이어 대칭([[combat]]의 `IDamageable` 계승) |

**하이라이트 패턴**
- **Observer로 도메인 결합 제거**: 적이 처치를 이벤트로 알리고, 브리지가 구독. 적은 성장을 모른다.
- **브리지 어댑터**: 두 독립 도메인을 잇는 유일한 지점을 한 클래스로 격리 — 배선 변경을 국소화.
- **Disposable 수명 관리**: 구독형 어댑터가 `IDisposable` → `PlayerRoot`가 파괴 시 해제해 이벤트 누수 차단.

## 9. Unity 특화

- **정적 허브 도메인 리로드 리셋**: `EnemyKillReward`는 `static event`라 "Enter Play Mode Options"(도메인 리로드 비활성) 시 이전 세션 구독자가 잔류할 수 있다. `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`으로 `Rewarded`를 초기화한다([[combat]] §11의 정적 상태 잔류 주의와 동일 결).
- **발행 시점 계약**: `Die()` 안, `SetActive(false)` **전** 발행(§6.2). 순서가 뒤바뀌면 비활성화 부작용과 얽힐 수 있다.
- **순수 C# 브리지**: `EnemyExpRewardHandler`는 MonoBehaviour가 아님 → `PlayerRoot`가 `ComposeCore`에서 생성, `OnDestroy`에서 Dispose(기존 `_deathHandler`·`_hitReaction`과 동일 수명).
- **성능 예산**: 처치 시에만 이벤트 1회 발행. 매 프레임 비용 0, 프레임당 GC Alloc 없음(`Action<int>` 값 전달).

## 10. 테스트 케이스

| 대상 | 확인 항목 |
|------|-----------|
| 보상 전달 | `Publish(N)` → 구독한 `IExpReceiver.AddExp(N)` 1회 호출 |
| 0/음수 가드 | `Publish(0)`·`Publish(-x)` → 전달 없음 |
| 다중 처치 누적 | 연속 `Publish` → 수신 합산(허브 무상태) |
| 구독 해제 | `Dispose` 후 `Publish` → 전달 없음(누수 차단) |
| 발행 시점 | `EnemyUnit.Die()`가 `SetActive(false)` 전에 `Publish` 호출 |
| null 방어 | `EnemyExpRewardHandler(null)` → `ArgumentNullException` |

> 브리지·허브는 목 `IExpReceiver`로 EditMode 단독 검증 가능(성장 스택 전체 구성 불필요).

## 11. 리스크·미결정(TBD)

- **성장 루프의 다음 단절점(레벨→스탯 리졸버)**: 경험치가 들어와 레벨이 올라도, `PlayerBaseStatResolver`가 레벨을 무시해 **스탯이 안 오른다**([[progression]] §11, `docs/reports/base-stat-resolver-level-scaling.md`). 이 배선이 끝나면 "레벨은 오르는데 강해지지 않음"이 곧바로 체감된다 → **다음 우선 과제**.
- **"누가 죽였는가" 미추적**: 단일 플레이어 가정이라 모든 처치를 그 플레이어의 경험치로 본다. 멀티/소환수 킬 귀속이 필요해지면 발행에 가해자 정보를 실어야 함([[combat]]의 `PlayerRegistry` 단일 플레이어 가정과 동일 한계).
- **정적 허브 잔류**: `PlayerRegistry`·`EnemyRegistry`와 같은 정적 상태 트레이드오프. 리셋 훅으로 완화하나, 멀티플레이 확장 시 서비스 주입으로 대체 필요.
- **세이브 부재**: 획득 경험치·레벨은 재기동 시 초기화된다(영속성 별도 과제). 지금은 런타임 진행만.

## 12. 확장 여지 (지금 만들지 않되 막지 않을 것)

- **`EnemyStat` SO 통합**: `expReward`·`maxHp`를 `EnemyStat`으로 이관해 적 능력치를 데이터로 일원화(현재 `EnemyUnit`이 SO 미사용).
- **보상 컨텍스트 확장**: 발행 페이로드를 `int`에서 구조체(위치·적 종류·킬 카운트)로 넓혀 데미지 팝업·퀘스트·드랍을 같은 허브로 확장(기존 구독자 불변).
- **경험치 획득 배율**: `ExpGainRate` 스탯([[progression]] §12)을 브리지 또는 `AddExp` 경로에 곱해 성장 가속 아이템 지원.
- **골드·드랍 보상**: 처치 보상의 형제 이벤트로 동일 패턴 재사용.

## 13. 신규/수정 파일 요약

| 구분 | 파일 | 위치(제안) |
|------|------|------------|
| 신규 | `EnemyKillReward.cs` | `Features/Enemy` |
| 신규 | `IExpReceiver.cs` | `Features/Player/Progression` |
| 신규 | `EnemyExpRewardHandler.cs` | `Features/Player/Progression` |
| 수정 | `EnemyUnit.cs` | `expReward` 필드 + `Die()`에서 `Publish` |
| 수정 | `PlayerProgressionController.cs` | `: IExpReceiver` 구현(기존 `AddExp` 재사용) |
| 수정 | `PlayerRoot.cs` | 브리지 생성(`ComposeCore`)·Dispose(`OnDestroy`) 배선 |
| 수정 | `docs/specs/player/progression.md` | 경험치 공급원 반영(§2·§7·§11·§13) — 같은 PR |
