using System;
using System.Collections.Generic;

public static class StatCatalog
{
    private static Dictionary<StatType, StatDefinition> Definitions = new()
    {
        { StatType.MaxHp,            new StatDefinition(StatType.MaxHp, 100f, 1f) },
        { StatType.HpRegen,          new StatDefinition(StatType.HpRegen, 1f, 0f) },
        { StatType.MaxMp,          new StatDefinition(StatType.MaxMp, 50f, 0f) },
        { StatType.MpRegen,        new StatDefinition(StatType.MpRegen, 1f, 0f) },
        { StatType.AttackPower,      new StatDefinition(StatType.AttackPower, 10f, 0f) },
        { StatType.AttackSpeed,      new StatDefinition(StatType.AttackSpeed, 1f, 0.05f, 10f) },
        { StatType.CritChance,       new StatDefinition(StatType.CritChance, 0.05f, 0f, 1f) },
        { StatType.CritDamage,       new StatDefinition(StatType.CritDamage, 1.5f, 1f, 10f) },
        { StatType.MoveSpeed,        new StatDefinition(StatType.MoveSpeed, 5f, 0f, 100f) },
        { StatType.Defense,          new StatDefinition(StatType.Defense, 0f, 0f) },
        { StatType.DamageReduction,  new StatDefinition(StatType.DamageReduction, 0f, 0f, 0.95f) },
        { StatType.LifeSteal,        new StatDefinition(StatType.LifeSteal, 0f, 0f, 1f) },
        { StatType.Range,            new StatDefinition(StatType.Range, 3f, 0.1f, 100f) },
        { StatType.CooldownReduction,new StatDefinition(StatType.CooldownReduction, 0f, 0f, 0.8f) },
        { StatType.Accuracy,         new StatDefinition(StatType.Accuracy, 1f, 0f, 100f) },
        { StatType.Evasion,          new StatDefinition(StatType.Evasion, 0f, 0f, 0.95f) },
        { StatType.ProjectileCount,  new StatDefinition(StatType.ProjectileCount, 1f, 1f, 100f) },
        { StatType.PickupRange,      new StatDefinition(StatType.PickupRange, 1.5f, 0f, 100f) },
        { StatType.ExpGainRate,      new StatDefinition(StatType.ExpGainRate, 1f, 0f, 100f) },
        { StatType.GoldGainRate,     new StatDefinition(StatType.GoldGainRate, 1f, 0f, 100f) },
    };

    public static StatDefinition Get(StatType type)
    {
        if (!Definitions.TryGetValue(type, out var def))
            throw new ArgumentOutOfRangeException(nameof(type), $"No definition for stat type: {type}");
        return def;
    }
    public static IEnumerable<StatType> AllStatTypes => Definitions.Keys;
}