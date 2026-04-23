public static class ModifierFactory
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
        return new StatModifier();
    }
}