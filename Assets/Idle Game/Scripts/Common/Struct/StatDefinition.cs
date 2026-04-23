public readonly struct StatDefinition
{
    public readonly StatType StatType;
    public readonly float DefaultBaseValue;
    public readonly float MaxValue;
    public readonly float MinValue;

    public StatDefinition(
        StatType statType,
        float defaultBaseValue,
        float minValue = float.MinValue,
        float maxValue = float.MaxValue)
    {
        StatType = statType;
        DefaultBaseValue = defaultBaseValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public float Clamp(float value)
    {
        if (value > MaxValue) return MaxValue;
        if (value < MinValue) return MinValue;
        return value;
    }
}