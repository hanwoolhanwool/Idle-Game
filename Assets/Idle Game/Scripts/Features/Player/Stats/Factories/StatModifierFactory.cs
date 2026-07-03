public static class StatModifierFactory
{
    public static StatModifier Add(
        StatType statType,
        float value,
        ModifierLayer layer,
        int order,
        object source,
        string sourceId
    )
    {
        return new StatModifier(statType,ModifierOp.Add,layer,value,order,source,sourceId);
    }

    public static StatModifier MulAdd(
        StatType statType,
        float value,
        ModifierLayer layer,
        int order,
        object source,
        string sourceId)
    {
        return new StatModifier(statType,ModifierOp.MultiplyAdditive, layer,value,order,source,sourceId);
    }
    
    public static StatModifier Mul(
        StatType statType,
        float multiplier,
        ModifierLayer layer,
        int order,
        object source,
        string sourceId)
    {
        return new StatModifier(statType, ModifierOp.Multiply, layer, multiplier, order, source, sourceId);
    }

    public static StatModifier Override(
        StatType statType,
        float value,
        ModifierLayer layer,
        int order,
        object source,
        string sourceId)
    {
        return new StatModifier(statType, ModifierOp.Override, layer, value, order, source, sourceId);
    }
}