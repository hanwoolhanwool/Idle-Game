using System.Collections.Generic;
using System.Linq;

public sealed class PlayerEquipmentController
{
    private readonly PlayerStatOrchestrator _orchestrator;
    private readonly Dictionary<string, EquipmentDefinition> _equippedByItemId = new();
    

    public PlayerEquipmentController(PlayerStatOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public void Initialize(IEnumerable<EquipmentDefinition> startEquipments)
    {
        if (startEquipments == null)
            return;
        foreach (var definition in startEquipments)
            Equip(definition);
    }

    public void Equip(EquipmentDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
            return;
        if (_equippedByItemId.ContainsKey(definition.ItemId))
            return;
        
        _equippedByItemId.Add(definition.ItemId, definition);
        _orchestrator.ApplyEquipment(definition);
    }

    public void Unequip(string itemId)
    {
        if (!_equippedByItemId.TryGetValue(itemId, out var definition))
            return;
        _equippedByItemId.Remove(itemId);
        _orchestrator.RemoveEquipment(definition);
    }

    public IReadOnlyCollection<EquipmentDefinition> GetEquippedSnapshot()
    {
        return _equippedByItemId.Values.ToList();
    }
}