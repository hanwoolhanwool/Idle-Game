# 리팩터링 제안 — 미사용·중복 모델 정리와 변환 로직 단일화

> 작성일: 2026-07-10
> 대상: `PlayerProgressionData`, `TimedBuffData`, `EquipmentRuntimeData`, `EquipmentFactory`, `StatModifierFactory`
> 성격: **분석·제안 문서** (코드 변경 없음)
> 관련 명세: [stats.md §11](../specs/player/stats.md) · [equipment.md §11](../specs/player/equipment.md) · [buffs.md §11](../specs/player/buffs.md) · [progression.md §11](../specs/player/progression.md)

---

## 1. 문제 요약

성장 요인(레벨·장비·버프)을 스탯에 반영하는 실제 경로는 **`PlayerStatOrchestrator` 하나**로 수렴한다. 그런데 그 경로가 쓰지 않는 **평행 모델·팩토리 5종**이 코드에 남아 있다. 일부는 과거 설계의 잔재이고, 일부는 `Orchestrator`와 **같은 변환 로직을 중복** 구현한다. 죽은 코드는 "이게 실제 경로인가?"라는 오해를 낳고, 중복 변환은 향후 수정 시 한쪽만 고치는 불일치 위험을 만든다.

## 2. 근거 (코드)

### 2-1. 순수 미사용 (죽은 코드)

| 타입 | 상태 | 대체물 |
|------|------|--------|
| `PlayerProgressionData` | 참조 0건 | `PlayerProgressionState`가 실제 상태 보유 |
| `TimedBuffData` | 참조 0건 | `BuffRuntimeInstance`가 실제 런타임 버프 보유 |

`PlayerProgressionData`는 필드 오타까지 있다: `BaseAttakPower`, `BaseAttakSpeed`, `BaseDefence`.

```csharp
// PlayerProgressionData.cs — 미사용 + 오타
public float BaseAttakPower;   // Attack
public float BaseAttakSpeed;   // Attack
public float BaseDefence;      // Defense
```

### 2-2. 변환 로직 중복

실제 경로는 `Orchestrator`가 `RuntimeModifierEntry[]` → `StatModifier`를 **직접** 변환한다:

```csharp
// PlayerStatOrchestrator.ApplyRuntimeModifiers() — 실제 사용되는 변환
var modifier = new StatModifier(
    entry.StatType, entry.Operation, entry.Layer,
    entry.Value, entry.Order, null, sourceId);
_statComponent.Stats.AddModifier(modifier);
```

그런데 **똑같은 변환**을 `EquipmentFactory`가 별도로 구현해 두고 아무도 부르지 않는다:

```csharp
// EquipmentFactory.DefinitionToRuntimeData() — 동일 변환, 호출처 없음
var modifier = new StatModifier(
    entry.StatType, entry.Operation, entry.Layer,
    entry.Value, entry.Order, null, runtimeData.ItemId);
runtimeData.Modifiers.Add(modifier);
```

`EquipmentRuntimeData`는 이 팩토리의 산출물이며, 자기 주석에 정리 필요를 명시한다:

```csharp
// EquipmentRuntimeData.cs
// 반드시 리펙터링 필요
// 장비 부위를 구분할 수 있는 머신 필요하다.
```

`StatModifierFactory`(`Add`/`MulAdd`/`Mul`/`Override` 헬퍼)도 참조 0건 — `Orchestrator`가 `new StatModifier(...)`를 직접 쓰기 때문이다.

## 3. 영향

| 관점 | 영향 |
|------|------|
| **가독성** | 신규 기여자가 "장비는 `EquipmentFactory`로 변환되는가?"라고 오해. 실제 경로 파악에 시간 낭비 |
| **유지보수** | 변환 규칙(예: `SourceId` 포맷, 새 modifier 필드) 변경 시 `Orchestrator`와 `EquipmentFactory` 중 한쪽만 고치면 불일치 |
| **오타 전파** | `PlayerProgressionData`의 오타 필드명이 복사되면 API로 굳어질 위험 |
| **빌드 위생** | 죽은 타입이 IntelliSense·검색 결과를 오염 |

## 4. 리팩터링 방안

각 타입을 **삭제 / 활용 승격 / 보류** 중 하나로 처분한다. 판단 축은 "확장 로드맵에서 실제로 쓸 것인가"다.

### 4-1. 즉시 삭제 (로드맵에 근거 없음)

| 타입 | 처분 | 근거 |
|------|------|------|
| `PlayerProgressionData` | **삭제** | `PlayerProgressionState`로 완전 대체. 오타 필드까지 존치 이유 없음 |
| `TimedBuffData` | **삭제** | `BuffRuntimeInstance`로 완전 대체 |
| `StatModifierFactory` | **삭제 또는 승격**(§4-2) | 현행 미사용 |

### 4-2. 변환 단일화 — 두 갈래

핵심은 `RuntimeModifierEntry[]` → `StatModifier[]` 변환의 **단일 지점(SoT)** 확립이다.

**안 G1 (권장, 최소): 팩토리 계열 삭제 + `Orchestrator` 유지**

- `EquipmentFactory`·`EquipmentRuntimeData`·`StatModifierFactory` 삭제.
- 변환은 `Orchestrator.ApplyRuntimeModifiers` 한 곳으로 확정.
- 가장 적은 코드로 중복·죽은 코드를 동시에 제거.

**안 G2 (구조 개선): 변환을 팩토리로 승격**

- `RuntimeModifierEntry[]` + `sourceId` → `StatModifier[]` 변환을 **`StatModifierFactory`(또는 정적 확장)로 추출**.
- `Orchestrator`가 인라인 변환 대신 이 팩토리를 호출.
- 변환 규칙을 한 타입에 모아 테스트 용이. 단 `EquipmentRuntimeData`/`EquipmentFactory`는 여전히 삭제(중간 표현이 불필요).

```csharp
// G2 스케치: 변환을 한 곳에 모음
public static class StatModifierFactory
{
    public static IEnumerable<StatModifier> FromEntries(
        RuntimeModifierEntry[] entries, string sourceId)
    {
        if (entries == null) yield break;
        foreach (var e in entries)
            yield return new StatModifier(
                e.StatType, e.Operation, e.Layer, e.Value, e.Order, null, sourceId);
    }
}
// Orchestrator는 FromEntries(entries, sourceId)를 순회해 AddModifier
```

> **권장**: 당장은 **G1**로 죽은 코드를 걷어내고, 변환 규칙이 복잡해질 조짐(조건부 modifier·부위별 처리)이 보이면 **G2**로 승격. 지금 G2를 강제하면 얇은 래퍼에 불과해 과설계 소지.

### 4-3. 보류(활용 가능성 있음) — `EquipmentRuntimeData` 계열

`EquipmentRuntimeData`의 주석이 가리키는 **"장비 부위(슬롯) 머신"** 은 [[equipment.md]] §11·§12의 실제 확장 항목이다. 다만:

- 현재 형태(ItemId + Modifiers 목록)는 부위 개념이 **없어** 그 확장을 직접 지원하지 못한다.
- 따라서 "미래 대비"로 지금 남겨두는 것은 **YAGNI 위반** — 부위 슬롯을 설계할 때 그에 맞는 모델을 새로 만드는 편이 낫다.
- **결론: 지금 삭제.** 부위 슬롯은 [[equipment.md]] §12의 별도 작업으로, 요구가 확정될 때 전용 모델과 함께 도입.

## 5. 처분 요약표

| 타입 | 권장 처분 | 대안 |
|------|-----------|------|
| `PlayerProgressionData` | 삭제 | — |
| `TimedBuffData` | 삭제 | — |
| `EquipmentRuntimeData` | 삭제 | 부위 슬롯 도입 시 재설계 |
| `EquipmentFactory` | 삭제 | — |
| `StatModifierFactory` | 삭제(G1) | 변환 승격(G2) 시 유지·재작성 |

## 6. 노력 / 리스크

| 항목 | 평가 |
|------|------|
| 노력 | **소** — G1은 파일 5개 삭제 수준. G2는 팩토리 1개 추가 + `Orchestrator` 내부 교체 |
| 리스크 | **낮음** — 삭제 대상은 참조 0건(사전 grep 필수). `Orchestrator` 경로는 불변(G1) |
| 씬/프리팹 영향 | 없음(모두 순수 C# 런타임 타입, SO 아님) |

## 7. 검증 방법

- **사전 grep**: 각 타입명 참조가 **0건**임을 삭제 전 확인(테스트·에디터 코드 포함).
- **컴파일**: 삭제 후 에러 0.
- **회귀(G2 선택 시)**: 장비·버프 적용/해제로 스탯 최종값이 리팩터링 전과 동일한지 EditMode 단언([[stats.md]] §10 "Source 회수" 케이스 재사용).

## 8. 권장 결론

1. `PlayerProgressionData`·`TimedBuffData`·`EquipmentRuntimeData`·`EquipmentFactory` **삭제**.
2. `StatModifierFactory`는 **G1(삭제)** 기본, 변환 규칙 복잡화 조짐 시 **G2(변환 단일화 승격)**.
3. 이 작업으로 [[stats.md]]·[[equipment.md]]·[[buffs.md]]·[[progression.md]] 각 §11의 "미사용 중복 모델" TBD를 일괄 해소한다.
