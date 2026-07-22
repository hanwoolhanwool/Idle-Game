# Stat 시스템 분석 및 리펙터링 안내

> 작성일: 2026-06-30
> 대상: `Assets/Idle Game/Scripts/Features/Player/Stats` 및 `Shared` 의 Stat 관련 코드

---

## 0. 진행 상황 (Progress Log)

| 날짜 | 항목 | 상태 | 비고 |
|------|------|------|------|
| 2026-07-04 | 🔴 P0-① 초기화 이중 적용 + 베이스 스탯 소실 | ✅ 완료 | `PlayerRoot.Initialize` 의 `(D)` 경로 제거 → `RefillResourcesToMax()` 호출로 대체 |
| 2026-07-04 | 🔴 P0-② 버프 제거 sourceId 불일치 | ✅ 완료 | `PlayerStatComponent` 의 죽은 버프 메서드(`ApplyTimeBuff`/`RemoveTimeBuffByBuffId`) 제거로 자연 소멸. 실제 경로(Orchestrator)는 `"buff:{BuffId}"` 로 일치. **수행: Claude 직접 수정(순수 삭제) / 사용자: 개념 복기 완료(2026-07-04)** — 버그 본질은 "저장 태그 규칙 ≠ 제거 태그 규칙"(접두사 불일치는 그 한 사례) |
| — | 🟠 P1-③ `StatMath` 정렬 부작용 | ⏳ 예정 | 매 계산 시 원본 리스트 in-place 정렬 제거 |
| — | 🟠 P1 `PlayerStatComponent` 책임 분리 | ⏳ 예정 | 자원 풀/전투 공식 분리 |
| — | 🟠 P1 `ModifierLayer` 정책 확정 | ⏳ 예정 | 정렬용 유지 vs 그룹 곱연산 결정 필요 |
| — | 🟡 P2 변환 일원화·모델 통합·정리 | ⏳ 예정 | Factory 일원화, 죽은 코드 제거, 네임스페이스, 오타 |
| 2026-07-21 | 🟡 P2 일부 — `PlayerProgressionData` 삭제 | ✅ 완료 | 커밋 58906bd(M0 레벨 테이블 작업)에서 선행 삭제. `TimedBuffData`·`EquipmentFactory`·`StatModifierFactory`는 여전히 P2 대기 |

> **부수 효과**: P0 수정으로 `EquipmentFactory.DefinitionToRuntimeData`, ~~`PlayerProgressionData`~~(2026-07-21 삭제됨), `TimedBuffData`, `PlayerStatComponent` 의 일부 `using` 이 미사용 상태가 됨 → P2(변환 일원화) 단계에서 함께 제거 예정.

---

## 1. 시스템 개요

### 1.1 구성 요소 한눈에 보기

| 계층 | 클래스 | 역할 |
|------|--------|------|
| **값 정의 (Shared)** | `StatType` (enum) | 스탯 종류 (MaxHp, AttackPower …) |
| | `ModifierOp` (enum) | 연산 방식 (Add / MultiplyAdditive / Multiply / Override) |
| | `ModifierLayer` (enum) | 출처 계층 (Base / Equipment / Buff …) |
| | `StatModifier` (struct) | 단일 보정값 (불변) |
| | `StatDefinition` (struct) | 스탯의 기본값/최소/최대 |
| | `RuntimeModifierEntry` (struct) | 직렬화용 모디파이어 (ScriptableObject 입력) |
| **계산 코어** | `StatCatalog` | 모든 `StatDefinition` 등록소 |
| | `StatMath` | 모디파이어 → 최종값 계산 로직 |
| | `StatMachine` | 베이스값 + 모디파이어 보관, 캐싱/이벤트 |
| | `StatSnapshot` | 특정 시점 전체 스탯 복사본 |
| **런타임** | `PlayerStatComponent` | HP/MP 자원, 데미지·힐·DPS 계산, 틱 |
| **오케스트레이션** | `PlayerStatOrchestrator` | Definition → Modifier 변환 후 StatMachine에 주입 |
| **컨트롤러** | `PlayerProgressionController` | 레벨/경험치 → 베이스 스탯 |
| | `PlayerEquipmentController` | 장착/해제 |
| | `PlayerBuffController` | 버프 적용/만료 |
| | `PlayerCombatController` | 전투 진입점 (데미지 위임) |
| **팩토리/리졸버** | `EquipmentFactory` | Definition → RuntimeData |
| | `StatModifierFactory` | StatModifier 생성 헬퍼 |
| | `PlayerBaseStatResolver` | Progression → BaseStatSet |
| **합성** | `PlayerRoot` (MonoBehaviour) | 전체 객체 생성·연결·틱 |
| **표현** | `PlayerHudBinder` | 스탯 변경 로그 출력 |

### 1.2 의도된 데이터 흐름

```
PlayerRoot (Compose / Initialize)
   │
   ├─ ProgressionController ─┐
   ├─ EquipmentController  ──┼─→ PlayerStatOrchestrator ─→ StatMachine.AddModifier / UpdateBaseValue
   ├─ BuffController       ──┘                                      │
   │                                                                ↓
   └─ PlayerStatComponent (HP/MP 자원, 데미지 계산)  ←──  StatMachine.GetFinal / OnStatChanged
                                                                    │
                                                          PlayerHudBinder (구독)
```

`StatMachine` 이 단일 진실 공급원(Single Source of Truth)이 되도록 설계된 구조이며,
계산은 dirty-flag 기반 캐싱으로 최적화되어 있습니다. **설계 방향 자체는 좋습니다.**

---

## 2. 핵심 강점

- **불변 `StatModifier` + 출처(sourceId) 기반 제거**: 버프/장비를 출처 단위로 깔끔히 add/remove.
- **dirty-flag 캐싱**: `GetFinal` 호출 시 변경된 스탯만 재계산. `StatSnapshot` 도 별도 dirty 관리.
- **`ModifierLayer` / `Order` 정렬 규칙**: 연산 순서를 명시적으로 통제하려는 의도가 있음.
- **계층 분리 시도**: Orchestrator를 통해 Definition과 StatMachine을 분리하려는 방향이 옳음.

---

## 3. 발견된 문제점 (우선순위순)

### 🔴 P0 — 치명적 버그: 초기화 시 데이터 이중 적용 + 베이스 스탯 소실  ✅ 해결됨 (2026-07-04)

`PlayerRoot.Initialize()` (PlayerRoot.cs:78) 에 **서로 다른 두 개의 적용 경로가 동시에 실행**됩니다.

```csharp
private void Initialize()
{
    _progressionController.Initialize();          // (A) Orchestrator → 정상 베이스 스탯 주입
    if (startEquipments != null)
        _equipmentController.Initialize(startEquipments);  // (B) Orchestrator → 장비 1차 적용
    if (startBuffs != null)
        for (...) _buffController.Apply(startBuffs[i]);     // (C) 버프 적용

    var equipData = EquipmentFactory.DefinitionToRuntimeData(startEquipments);
    _statComponent.Initialize(new PlayerProgressionData(), equipData);  // (D) ⚠️ 문제
    ...
}
```

`(D)` 의 `_statComponent.Initialize(...)` 내부(`PlayerStatComponent.cs:23`)는:

1. **`ApplyProgression(new PlayerProgressionData())`** → `PlayerProgressionData` 의 모든 필드가 **기본값 0**.
   → `(A)` 에서 올바르게 넣은 MaxHp/AttackPower 등 베이스 스탯이 **전부 0 으로 덮어써짐.**
2. **`ApplyEquipments(equipData)`** → `(B)` 에서 이미 적용한 장비 모디파이어가 **두 번째로 중복 적용됨.**

**결과**: 게임 시작 시 베이스 스탯은 0, 장비 효과는 2배가 됩니다. 시스템의 가장 심각한 결함입니다.

> 근본 원인: `PlayerStatComponent` 와 컨트롤러 그룹(Orchestrator 경유)이 **둘 다 StatMachine에 직접 쓰는 두 개의 독립 경로**를 가지고 있습니다.

---

### 🔴 P0 — 버프 제거 sourceId 불일치  ✅ 해결됨 (2026-07-04)

- 적용: `PlayerStatOrchestrator.ApplyBuff` → sourceId = `"buff:{BuffId}"` (Orchestrator.cs:43)
- 제거(별도 경로): `PlayerStatComponent.RemoveTimeBuffByBuffId` → `RemoveModifierBySourceId(buffData.BuffId)` (StatComponent.cs:91) — **`"buff:"` 접두사 없음.**

`PlayerStatComponent.ApplyTimeBuff` 도 `TimedBuffData.Modifiers` 의 sourceId를 그대로 사용하므로,
이 경로로 적용된 버프는 **영구히 제거 불가**합니다. (현재 `PlayerBuffController` 가 Orchestrator 경로를 쓰므로 죽은 코드일 가능성이 높지만, 혼란과 잠재 버그의 원인.)

---

### 🟠 P1 — 책임 과다: `PlayerStatComponent` 의 SRP 위반

`PlayerStatComponent` 가 다음을 **전부** 떠안고 있습니다 (코드 주석에도 "리펙터링 필수" 명시):

- HP/MP 자원 보관 및 회복 틱
- 데미지/힐/MP 소비 계산
- DPS·치명타 데미지 계산 (전투 공식)
- 버프 적용/제거 (`ApplyTimeBuff` / `RemoveTimeBuff`)
- 장비 적용 (`ApplyEquipments`)
- progression 적용 (`ApplyProgression`)

→ 버프·장비·progression은 이미 전용 컨트롤러가 있으므로 **중복**이며, 자원·전투 공식은 별도 클래스로 분리되어야 합니다.

---

### 🟠 P1 — `StatMath.Calculate` 의 부작용(원본 정렬)

```csharp
modifiers.Sort(ModifierComparer.Instance);   // StatMath.cs:13 — 전달받은 원본 List를 직접 정렬
```

`GetFinal` 이 호출될 때마다(매 dirty 재계산) **StatMachine이 보유한 원본 리스트를 in-place 정렬**합니다.
순수 함수여야 할 계산 메서드가 입력을 변형하는 것은 위험합니다. 또한 매번 정렬은 불필요한 비용입니다.

---

### 🟠 P1 — `ModifierLayer` 가 실질적으로 미동작

`ModifierLayer` 는 정렬 키로만 쓰이고, **계산식에서 레이어별 곱연산 그룹화가 없습니다.**
`MultiplyAdditive` 는 출처와 무관하게 전부 한 덩어리(`additiveMulSum`)로 합산된 뒤 한 번만 곱해집니다.

```csharp
value *= (1f + additiveMulSum);   // 모든 레이어의 MultiplyAdditive가 한 그룹으로 섞임
```

→ "장비 합연산 그룹"과 "버프 합연산 그룹"을 분리하려던 의도(레이어 개념)가 실제로는 구현되지 않았습니다.
레이어를 단순 정렬용으로 둘 것인지, 그룹 곱연산으로 갈 것인지 **설계 결정이 필요**합니다.

---

### 🟡 P2 — 변환 경로 3중화 및 모델 중복

- 같은 "Definition → Modifier" 변환이 **3곳**에 흩어져 있음:
  `PlayerStatOrchestrator.ApplyRuntimeModifiers`, `EquipmentFactory.DefinitionToRuntimeData`, (`PlayerStatComponent.ApplyEquipments` 의 RuntimeData 소비).
- DTO 중복: ~~`PlayerProgressionData` ≈ `PlayerBaseStatSet`~~ (전자는 2026-07-21 삭제 완료 — §0 진행 로그 참조).
- 버프 런타임: `TimedBuffData`(List<StatModifier>) vs `BuffRuntimeInstance`(RuntimeModifierEntry[]) 가 공존.

→ 변환 로직을 **한 곳(Factory)** 으로 모으고, 사용되지 않는 모델을 제거해야 합니다.

---

### 🟡 P2 — 기타

| 위치 | 내용 |
|------|------|
| `StatMachine.OnSnapshotChanged` | `ForceRecalculateAll` 에서만 발화. 일반 변경 시 스냅샷 이벤트가 안 옴 → 사실상 미사용. |
| `PlayerHudBinder.HandleStatChanged` | 스탯이 하나 바뀔 때마다 전체 HUD를 갱신(매번 Debug.Log). 다수 모디파이어 일괄 적용 시 N번 호출. |
| `StatSnapshot` | 필드 `_stats = new Dictionary()` 초기화 후 생성자에서 즉시 덮어씀(불필요). `AsReadOnly()` 가 내부 dict를 그대로 노출. |
| `EquipmentRuntimeData` 주석 | "장비 부위 구분 머신 필요" — 같은 슬롯 교체 로직 부재. |
| 네임스페이스 | 모든 클래스가 **전역 네임스페이스**. 프로젝트 규모가 커지면 충돌·탐색 비용 증가. |
| 미사용 using | `PlayerStatComponent` 의 `Unity.Mathematics`, `UnityEngine` 등 불필요 import. |
| 오타 다수 | `BaseAttakPower`, `BaseAttakSpeed`, `BaseDefence`, `overridValue`, `Opthinal Presenters`, `debugLogOrRefresh`, `HandleStateChanged`(→StatChanged). 공개 API/직렬화 필드라 수정 시 주의(에셋 재바인딩 필요). |

---

## 4. 리펙터링 가이드

### 4.1 즉시 수정 (버그 — 코드만으로 동작 정상화)

#### ① `PlayerRoot.Initialize` 의 이중 경로 제거 (P0)

`(D)` 의 `_statComponent.Initialize(...)` 호출을 **삭제**하고, `PlayerStatComponent` 에서 progression/equipment 적용 책임을 제거합니다. 자원 초기 충전만 별도 메서드로 남깁니다.

```csharp
private void Initialize()
{
    _progressionController.Initialize();                 // 베이스 스탯
    _equipmentController.Initialize(startEquipments);    // 장비 (null 체크는 컨트롤러 내부에 이미 있음)
    if (startBuffs != null)
        foreach (var buff in startBuffs)
            _buffController.Apply(buff);

    _statComponent.RefillResourcesToMax();   // 모든 모디파이어 적용 후 HP/MP를 Max로
    hudBinder?.Bind(_statComponent);
}
```

→ `PlayerStatComponent` 에서 `Initialize/ApplyProgression/ApplyEquipments/ApplyTimeBuff/RemoveTimeBuffByBuffId` 제거,
`RefillResourceToCapOnInitialize` 를 public `RefillResourcesToMax` 로 노출.

#### ② 버프 sourceId 일치 (P0)

`PlayerStatComponent` 의 버프 메서드를 제거하면 이 버그는 자연 소멸합니다.
유지해야 한다면 제거 시에도 동일하게 `$"buff:{buffData.BuffId}"` 를 사용하도록 통일하세요.

#### ③ `StatMath` 부작용 제거 (P1)

원본을 정렬하지 말고, 정렬을 **모디파이어 추가 시점**으로 옮기거나(정렬 상태 유지), 계산 시 복사본을 정렬합니다.
권장은 "추가 시 정렬 위치를 유지(insert sorted)" 또는 "StatMachine이 정렬된 상태를 보장"하는 방식 — 매 계산 정렬 비용 제거.

---

### 4.2 구조 개선 (단계적)

#### 단계 1 — 책임 분리

`PlayerStatComponent` 를 다음으로 분할:

- **`PlayerResourcePool`**: `_currentHp/_currentMp`, `Tick(regen)`, `ApplyDamage`, `Heal`, `TrySpendMp`, MaxHp/MaxMp 변동 시 클램프.
- **`CombatStatCalculator`** (또는 `PlayerCombatController` 로 흡수): `ComputeFinalDamagePerHit`, `ComputeDps`, 방어/감쇄 공식.

`PlayerStatComponent` 는 자원 풀에 대한 얇은 파사드로 축소하거나 제거.

#### 단계 2 — 변환 일원화

"Definition → StatModifier" 변환을 **`StatModifierFactory` / `EquipmentFactory` 한 곳**으로 모으고,
`PlayerStatOrchestrator.ApplyRuntimeModifiers` 도 이 팩토리를 호출하게 합니다.
~~`PlayerProgressionData` 를 제거하고 `PlayerBaseStatSet` 으로 통일.~~ (2026-07-21 삭제 완료)

#### 단계 3 — 레이어 정책 확정

`ModifierLayer` 를 (a) 단순 정렬용으로 유지할지, (b) 레이어별 곱연산 그룹으로 정식 구현할지 결정.
(b) 라면 `StatMath` 에서 레이어별로 `MultiplyAdditive` 를 그룹화해 각각 `(1 + Σ)` 를 곱하도록 변경.

#### 단계 4 — 이벤트/표현 정리

- 다수 모디파이어 일괄 적용 시 `BeginBatch/EndBatch` 패턴으로 변경 이벤트를 1회로 합치기(또는 `OnSnapshotChanged` 를 일반 변경에도 발화하도록 정비).
- `PlayerHudBinder` 가 스냅샷 1개를 받아 갱신하도록 변경.

#### 단계 5 — 정리 작업

- 네임스페이스 도입 (`Game.Player.Stats` 등).
- 오타 일괄 수정 (직렬화 필드는 `[FormerlySerializedAs]` 로 에셋 호환 유지).
- 미사용 using/모델/이벤트 제거.

---

## 5. 권장 진행 순서 요약

| 순서 | 작업 | 영향 | 위험도 | 상태 |
|------|------|------|--------|------|
| 1 | `PlayerRoot.Initialize` 이중 적용 제거 | 게임 시작 스탯 정상화 | 낮음 | ✅ 완료 (2026-07-04) |
| 2 | `PlayerStatComponent` 의 버프/장비/progression 메서드 제거 | 중복 경로 제거 | 낮음 | ✅ 완료 (2026-07-04) |
| 3 | `StatMath` 정렬 부작용 제거 | 안정성 | 낮음 | ⏳ 예정 |
| 4 | 자원/전투 공식 분리 (단계 1) | 구조 개선 | 중간 | ⏳ 예정 |
| 5 | 변환 일원화·모델 통합 (단계 2) | 유지보수성 | 중간 | ⏳ 예정 |
| 6 | 레이어 정책 확정 (단계 3) | 밸런싱 정확도 | 중간 | ⏳ 예정 |
| 7 | 이벤트/표현·정리 (단계 4–5) | 품질 | 낮음 | ⏳ 예정 |

> **1~3번은 즉시 진행 권장** (버그 수정, 위험 낮음).
> 4번 이후는 별도 브랜치에서 단계별 커밋을 권장합니다.

---

## 6. 부록 — 현재 계산식 정리

`StatMath.Calculate` 의 실제 동작:

```
1. 모디파이어를 (Layer → Order → Op) 순으로 정렬
2. value = baseValue
3. 순회:
     Add              → value += v
     MultiplyAdditive → additiveMulSum += v   (전 레이어 공통 누적)
     Multiply         → value *= v
     Override         → 마지막 값 기록(플래그)
4. value *= (1 + additiveMulSum)
5. Override 가 있으면 value = overrideValue   ← 모든 연산을 무시
6. Clamp(min, max)
```

⚠️ 주의: 정렬을 하지만 4단계의 `additiveMulSum` 일괄 곱과 5단계 Override는 **순서와 무관**하게 동작하므로,
현재 정렬은 `Add` ↔ `Multiply` 의 상대 순서에만 의미가 있습니다. (레이어 그룹화 미구현 — 3장 P1 참조)
