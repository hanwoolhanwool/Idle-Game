using System.Collections.Generic;

public static class EquipmentFactory
{
    public static List<EquipmentRuntimeData> DefinitionToRuntimeData(
        IEnumerable<EquipmentDefinition> definitions)
    {
        var runtimeDataList = new List<EquipmentRuntimeData>();

        if (definitions == null)
            return runtimeDataList;

        foreach (var definition in definitions)
        {
            var runtimeData = DefinitionToRuntimeData(definition);

            if (runtimeData != null)
                runtimeDataList.Add(runtimeData);
        }

        return runtimeDataList;
    }

    public static EquipmentRuntimeData DefinitionToRuntimeData(
        EquipmentDefinition definition)
    {
        if (definition == null)
            return null;

        if (string.IsNullOrWhiteSpace(definition.ItemId))
            return null;

        var runtimeData = new EquipmentRuntimeData
        {
            ItemId = definition.ItemId
        };

        if (definition.Modifiers == null)
            return runtimeData;

        for (int i = 0; i < definition.Modifiers.Length; i++)
        {
            RuntimeModifierEntry entry = definition.Modifiers[i];

            var modifier = new StatModifier(
                entry.StatType,
                entry.Operation,
                entry.Layer,
                entry.Value,
                entry.Order,
                null,
                runtimeData.ItemId
            );

            runtimeData.Modifiers.Add(modifier);
        }

        return runtimeData;
    }
}