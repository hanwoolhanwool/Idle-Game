public sealed class PlayerStatOrchestrator
{
    private readonly PlayerStatComponent _statComponent;

    public PlayerStatOrchestrator(PlayerStatComponent statComponent)
    {
        _statComponent = statComponent;
    }

    /// <summary>
    /// 성장에서 파생된 베이스 스탯을 StatMachine에 반영한다.
    /// 어떤 스탯이 성장에 참여하는지는 <see cref="PlayerLevelTable"/>이 결정하므로,
    /// 여기서는 집합을 순회만 한다(새 스탯이 늘어도 이 메서드는 바뀌지 않는다 — OCP).
    /// </summary>
    public void ApplyBaseStats(PlayerBaseStatSet baseStats)
    {
        if (baseStats == null) return;

        var stats = _statComponent.Stats;
        foreach (var entry in baseStats.Entries)
            stats.UpdateBaseValue(entry.Key, entry.Value);
    }

    public void ApplyEquipment(EquipmentDefinition definition)
    {
        if (definition == null) return;
        
        string sourceId = $"item:{definition.ItemId}";
        ApplyRuntimeModifiers(definition.Modifiers, sourceId);
    }

    public void RemoveEquipment(EquipmentDefinition definition)
    {
        if (definition == null) return;
        
        string sourceId = $"item:{definition.ItemId}";
        _statComponent.Stats.RemoveModifierBySourceId(sourceId);
    }

    public void ApplyBuff(BuffDefinition definition)
    {
        if (definition == null) return;
        
        string sourceId = $"buff:{definition.BuffId}";
        ApplyRuntimeModifiers(definition.Modifiers, sourceId);
    }

    public void RemoveBuff(string buffId)
    {
        if (string.IsNullOrWhiteSpace(buffId))
            return;
        
        string sourceId = $"buff:{buffId}";
        _statComponent.Stats.RemoveModifierBySourceId(sourceId);
    }

    private void ApplyRuntimeModifiers(RuntimeModifierEntry[] entries, string sourceId)
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            RuntimeModifierEntry entry = entries[i];
            var modifier = new StatModifier(
                entry.StatType,
                entry.Operation,
                entry.Layer,
                entry.Value,
                entry.Order,
                null,
                sourceId
                );
            
            _statComponent.Stats.AddModifier(modifier);
        }
    }
}