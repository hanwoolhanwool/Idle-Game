public sealed class PlayerStatOrchestrator
{
    private readonly PlayerStatComponent _statComponent;

    public PlayerStatOrchestrator(PlayerStatComponent statComponent)
    {
        _statComponent = statComponent;
    }

    public void ApplyBaseStats(PlayerBaseStatSet baseStats)
    {
        var stats = _statComponent.Stats;
        stats.UpdateBaseValue(StatType.MaxHp, baseStats.MaxHp);
        stats.UpdateBaseValue(StatType.MaxHp, baseStats.MaxHp);
        stats.UpdateBaseValue(StatType.AttackPower, baseStats.AttackPower);
        stats.UpdateBaseValue(StatType.AttackSpeed, baseStats.AttackSpeed);
        stats.UpdateBaseValue(StatType.MoveSpeed, baseStats.MoveSpeed);
        stats.UpdateBaseValue(StatType.Defense, baseStats.Defense);
        stats.UpdateBaseValue(StatType.MaxMp, baseStats.MaxMana);
        stats.UpdateBaseValue(StatType.HpRegen, baseStats.HpRegen);
        stats.UpdateBaseValue(StatType.MpRegen, baseStats.MpRegen);
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