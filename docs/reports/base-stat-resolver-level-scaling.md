# 리팩터링 제안 — 베이스 스탯 리졸버의 레벨 스케일링 구현

> 작성일: 2026-07-10
> 대상: `PlayerBaseStatResolver`, `IPlayerBaseStatResolver`, `PlayerProgressionConfig`
> 성격: **분석·제안 문서** (작성 시점 코드 변경 없음)
> 관련 명세: [progression.md §11](../specs/player/progression.md) · [stats.md](../specs/player/stats.md)

> **후기(2026-07-21) — 구현 완료.** 이 제안은 커밋 `58906bd`(리졸버)·`686a470`(PlayerLevelTable 에셋)·`2969838`(씬 연결)으로 구현되었다. 채택안은 **R3(테이블 SO)의 골격에 R1(선형 공식)을 담은 절충** — `PlayerLevelTable`이 경험치 곡선(`BaseRequiredExp × ExpGrowthRate^(level-1)`)과 스탯 성장(`BaseValue + PerLevel × (level-1)`)을 소유한다. §8의 "우선 R1, 이후 R3 승격" 경로를 한 번에 이행한 셈이다. 계약은 본문 예상과 달리 `Resolve(state)`로 바뀌어 **config 의존이 제거**되었다(config는 시작 상태 전용, 설계 근거: [m0-close-the-loop-plan.md §5.1](../design/m0-close-the-loop-plan.md)). 이하 본문은 구현 전 진단의 기록이다.

---

## 1. 문제 요약

레벨업은 정상 동작한다 — `PlayerProgressionController.AddExp`가 경험치를 누적해 `Level`을 올린다. 그러나 레벨이 올라도 **베이스 스탯이 실제로는 오르지 않는다.** 레벨→스탯 환산을 담당하는 `PlayerBaseStatResolver`가 레벨을 **무시**하고 `config`의 고정 시작값을 그대로 반환하기 때문이다. 방치형의 핵심 루프(성장→강해짐)가 스탯 축에서 끊겨 있다.

## 2. 근거 (코드)

**리졸버가 레벨을 무시하고 config를 그대로 반환:**

```csharp
// PlayerBaseStatResolver.cs
public PlayerBaseStatSet Resolve(PlayerProgressionState progressionState, PlayerProgressionConfig config)
{
    // 샘플은 config 직접 사용.
    // 실무에서는 레벨 테이블, 클래스, 승급, 연구 데이터를 합산해서 만든다.
    return new PlayerBaseStatSet
    {
        MaxHp       = config.StartMaxHp,        // ← progressionState.Level 미사용
        AttackPower = config.StartAttackPower,
        // ... 전부 Start* 고정값
    };
}
```

**레벨업 시 리졸버를 다시 부르지만 결과가 불변:**

```csharp
// PlayerProgressionController.cs
public void AddExp(int amount)
{
    _state.Exp += amount;
    while (_state.Exp >= RequiredExpForNextLevel(_state.Level))
    {
        _state.Exp -= RequiredExpForNextLevel(_state.Level);
        _state.Level++;               // 레벨은 오르지만…
    }
    RefreshBaseStats();               // …Resolve가 같은 값을 반환 → 스탯 불변
}
```

즉 배선(`Controller → Resolver → Orchestrator.ApplyBaseStats`)은 완결돼 있고, **비어 있는 것은 리졸버의 산출 공식뿐**이다. 계약 `IPlayerBaseStatResolver.Resolve(state, config)`는 이미 `state`(레벨 포함)를 받으므로, 구현만 채우면 된다.

## 3. 영향

| 관점 | 영향 |
|------|------|
| **게임플레이** | 레벨업 보상이 스탯에 반영되지 않음 → 방치 성장의 핵심 피드백 부재 |
| **정합성** | HUD·전투는 최종 스탯을 정확히 읽지만([[stats.md]]), 그 최종 스탯의 뿌리(base)가 성장하지 않음 |
| **파급** | `RefillResourcesToMax`가 레벨업 시 최대 HP를 늘려주지 못함 → 체력 성장 체감 0 |

배선·계약은 정상이라 **버그라기보다 미구현 샘플**이다. 리스크가 낮고 효과가 큰 전형적 "채우기" 작업.

## 4. 리팩터링 방안

`IPlayerBaseStatResolver` 계약은 유지하고 **구현 전략만** 채운다. 성장 곡선 표현 방식에 따라 3안.

### 안 R1 — 선형/계수 성장 (가장 단순)

`config`에 레벨당 증가량 필드를 추가하고 `base + (Level-1) × perLevel`로 산출.

```csharp
// PlayerProgressionConfig에 필드 추가 (예)
public float HpPerLevel = 10f;
public float AttackPerLevel = 2f;
// …

// Resolver
int lv = Mathf.Max(1, progressionState.Level);
return new PlayerBaseStatSet {
    MaxHp       = config.StartMaxHp      + (lv - 1) * config.HpPerLevel,
    AttackPower = config.StartAttackPower + (lv - 1) * config.AttackPerLevel,
    // …
};
```

- **장점**: 최소 코드, 밸런서가 계수만 조정. **단점**: 곡선이 선형에 고정(구간별 조정 불가).

### 안 R2 — 성장 계수(배율) 성장

레벨당 **배율**(예: 레벨당 +5%)로 지수 성장. 방치형의 후반 스케일에 적합.

```csharp
float mul = Mathf.Pow(1f + config.GrowthRatePerLevel, lv - 1);
MaxHp = config.StartMaxHp * mul;
```

- **장점**: 후반 성장감. **단점**: 스탯별 곡선을 하나의 배율로 묶으면 밸런스 자유도 제한.

### 안 R3 — 레벨 테이블 SO (가장 유연, 권장 상한)

레벨→스탯 매핑을 별도 `ScriptableObject`(레벨별 행)로 두고 리졸버가 조회·보간.

```csharp
// LevelStatTable(SO): 레벨 구간별 스탯 곡선을 에디터에서 편집
public sealed class LevelStatTable : ScriptableObject {
    public AnimationCurve MaxHpByLevel;      // 또는 레벨별 배열
    public AnimationCurve AttackByLevel;
    // …
}
```

- **장점**: 구간별 자유 곡선, 밸런서가 코드 없이 완전 제어. **단점**: 데이터·조회 코드 추가.

### 방안 비교

| 안 | 표현력 | 노력 | 밸런서 자유도 | 권장 상황 |
|----|--------|------|---------------|-----------|
| R1 선형 | 낮음 | 소 | 계수 | 프로토타입·초기 |
| R2 배율 | 중 | 소 | 배율 1개 | 지수 성장 필요 |
| **R3 테이블** | 높음 | 중 | 완전 | 정식 밸런싱 단계 |

> **권장 경로**: 지금은 **R1**로 성장 루프를 즉시 잇고(효과 확인), 밸런싱이 본격화되면 **R3**로 승격. 계약(`Resolve`)이 동일하므로 구현 교체만으로 무손실 이행([[progression.md]] §12 "실무 리졸버").

## 5. 설계 유지 포인트

- **계약 불변**: `IPlayerBaseStatResolver`를 그대로 두어 `PlayerProgressionController`·`PlayerRoot` 배선은 손대지 않는다(DIP 이점 실증).
- **base vs modifier**: 레벨 성장은 [[stats.md]] §6.4대로 **base 값**(`ApplyBaseStats`→`UpdateBaseValue`)으로 반영한다. 장비·버프 modifier와 층이 분리되어 회수·조합이 깨지지 않는다.
- **승급·연구 확장**: `Resolve`가 `state`(→`PromotionTier`)와 `config`를 함께 받으므로, 훗날 승급·연구 보정을 **같은 리졸버에서 합산**할 수 있다(주석이 예고한 방향).

## 6. 노력 / 리스크

| 항목 | 평가 |
|------|------|
| 노력 | R1/R2 **소**(리졸버 구현 + config 필드 몇 개) / R3 **중**(SO + 조회) |
| 리스크 | **낮음** — 계약·배선 불변. 산출 공식만 교체 |
| 씬/프리팹 영향 | R1/R2: `PlayerProgressionConfig` SO에 필드 추가(기존 에셋은 기본값). R3: 새 SO 에셋 작성·연결 |

## 7. 검증 방법

- **EditMode**: 목 `state.Level`을 1·5·10으로 주고 `Resolve` 결과가 곡선대로 증가하는지 단언.
- **통합**: `PlayerProgressionController.AddExp(대량)` → 레벨 상승 후 `StatMachine.GetFinal(MaxHp)`가 증가하는지 확인.
- **런타임(에디터)**: `PlayerDebugCommands.GainTestExp` 반복 → HUD의 MaxHp/ATK 상승 관찰([[presentation.md]]). 레벨업 후 `RefillResourcesToMax` 경로로 현재 HP도 새 최대치 반영되는지 체크.
- **회귀**: 레벨 1에서는 리팩터링 전과 동일한 시작 스탯(= `Start*`)이 나오는지(하위호환) 확인.

## 8. 권장 결론

1. `PlayerBaseStatResolver.Resolve`에 레벨 스케일링을 구현한다 — 우선 **R1(선형/계수)** 로 성장 루프를 즉시 연결.
2. 밸런싱 단계에서 **R3(레벨 테이블 SO)** 로 무손실 승격(계약 동일).
3. 이 작업으로 [[progression.md]] §11의 "샘플 리졸버가 레벨을 반영하지 않음" TBD를 해소한다.
