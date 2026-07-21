using System;
using UnityEngine;

/// <summary>
/// 레벨 성장 규칙의 단일 진실 공급원(SO).
/// "레벨 N일 때 필요 경험치는 얼마이고 베이스 스탯은 얼마인가"를 전적으로 여기서 답한다.
/// 성장 공식을 코드가 아닌 데이터에 두어 기획자가 코드 수정 없이 밸런스를 조정하게 한다.
/// (계획서: docs/design/m0-close-the-loop-plan.md §5.1)
/// </summary>
[CreateAssetMenu(menuName = "Game/Player/Level Table", fileName = "PlayerLevelTable")]
public sealed class PlayerLevelTable : ScriptableObject
{
    /// <summary>
    /// float 유효자리(7)가 무너지기 시작하는 2^24(약 1677만)의 60% 지점.
    /// 성장 곡선이 이 선을 넘으면 데미지 누락 같은 조용한 버그가 생긴다.
    /// (기획 제약: docs/gdd/content-roadmap.md §3.6)
    /// </summary>
    public const float SafeMagnitudeLimit = 1e7f;

    [Header("Level Cap")]
    [Min(1)]
    [Tooltip("최고 레벨. 이 값을 넘어서는 성장은 없다.")]
    public int MaxLevel = 100;

    [Header("Exp Curve — Required(level) = BaseRequiredExp * GrowthRate^(level-1)")]
    [Min(1)]
    [Tooltip("레벨 1 → 2에 필요한 경험치.")]
    public int BaseRequiredExp = 100;

    [Min(1f)]
    [Tooltip("레벨당 필요 경험치 배율. 1이면 매 레벨 동일, 1.12면 레벨마다 12% 증가.")]
    public float ExpGrowthRate = 1.12f;

    [Header("Stat Growth — Value(level) = BaseValue + PerLevel * (level-1)")]
    [Tooltip("레벨 성장에 참여할 스탯 목록. 여기 없는 스탯은 레벨로 오르지 않는다.")]
    public StatGrowthEntry[] Growths =
    {
        new() { Type = StatType.MaxHp,       BaseValue = 100f, PerLevel = 20f },
        new() { Type = StatType.MaxMp,       BaseValue = 50f,  PerLevel = 5f },
        new() { Type = StatType.AttackPower, BaseValue = 10f,  PerLevel = 5f },
        new() { Type = StatType.AttackSpeed, BaseValue = 1f,   PerLevel = 0.01f },
        // 이동속도는 레벨로 오르지 않는다(체감 이동감이 성장에 따라 변하면 조작이 불안정해진다).
        new() { Type = StatType.MoveSpeed,   BaseValue = 10f,  PerLevel = 0f },
        new() { Type = StatType.Defense,     BaseValue = 0f,   PerLevel = 2f },
        new() { Type = StatType.HpRegen,     BaseValue = 1f,   PerLevel = 0.1f },
        new() { Type = StatType.MpRegen,     BaseValue = 1f,   PerLevel = 0.1f },
    };

    /// <summary>
    /// <paramref name="level"/>에서 다음 레벨로 가는 데 필요한 경험치.
    /// 최고 레벨 이상이면 <see cref="int.MaxValue"/>를 반환해 더 이상 오를 수 없음을 표현한다.
    /// </summary>
    public int RequiredExp(int level)
    {
        if (level < 1)
            level = 1;

        if (level >= MaxLevel)
            return int.MaxValue;

        double required = BaseRequiredExp * Math.Pow(ExpGrowthRate, level - 1);

        // 곡선이 int를 넘길 만큼 가파르게 설정된 경우를 방어한다(설정 실수).
        return required >= int.MaxValue
            ? int.MaxValue
            : Mathf.Max(1, (int)Math.Round(required));
    }

    /// <summary>
    /// <paramref name="level"/>의 베이스 스탯 집합. 레벨은 [1, <see cref="MaxLevel"/>]로 클램프한다
    /// (세이브에 저장된 레벨이 밸런스 패치로 내려간 상한을 넘는 경우 방어).
    /// </summary>
    public PlayerBaseStatSet ResolveStats(int level)
    {
        int clamped = Mathf.Clamp(level, 1, MaxLevel);
        var set = new PlayerBaseStatSet();

        if (Growths == null)
            return set;

        for (int i = 0; i < Growths.Length; i++)
        {
            StatGrowthEntry growth = Growths[i];
            float value = growth.BaseValue + growth.PerLevel * (clamped - 1);
            set.Set(growth.Type, value);
        }

        return set;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 성장 곡선이 최고 레벨에서 float 안전 상한을 넘지 않는지 에디터에서 검증한다.
    /// 런타임에 조용히 깨지는 대신 설정 시점에 드러내는 편이 싸다.
    /// </summary>
    private void OnValidate()
    {
        if (Growths == null)
            return;

        PlayerBaseStatSet atMax = ResolveStats(MaxLevel);
        foreach (var pair in atMax.Entries)
        {
            if (pair.Value > SafeMagnitudeLimit)
            {
                Debug.LogWarning(
                    $"[{name}] {pair.Key}가 최고 레벨({MaxLevel})에서 {pair.Value:N0}이 되어 " +
                    $"float 안전 상한({SafeMagnitudeLimit:N0})을 넘습니다. 성장 계수를 낮추세요.", this);
            }
        }
    }
#endif
}

/// <summary>
/// 스탯 하나의 레벨 성장 규칙. 선형(BaseValue + PerLevel × 경과 레벨)으로 둔 이유는
/// 지수 성장이 float 안전 상한을 순식간에 넘기기 때문이다(유한형 기획 결정).
/// </summary>
[Serializable]
public struct StatGrowthEntry
{
    [Tooltip("성장시킬 스탯.")]
    public StatType Type;

    [Tooltip("레벨 1에서의 값.")]
    public float BaseValue;

    [Tooltip("레벨이 1 오를 때마다 더해지는 값.")]
    public float PerLevel;
}
