public readonly struct StatModifier
{
    public readonly StatType StatType;
    public readonly ModifierOp Op;
    public readonly ModifierLayer Layer;
    public readonly float Value;
    public readonly int Order;
    public readonly object Source;
    public readonly string SourceId;

    public StatModifier(
        StatType statType,
        ModifierOp op,
        ModifierLayer layer,
        float value,
        int order,
        object source,
        string sourceId)
    {
        StatType = statType;
        Op = op;
        Layer =  layer;
        Value =  value;
        Order = order;
        Source = source;
        SourceId = sourceId ?? string.Empty;
    }

    public override string ToString()
    {
        return $"{StatType} | {Op} | {Layer} | {Value} | order:{Order} | source:{SourceId}";
    }
}