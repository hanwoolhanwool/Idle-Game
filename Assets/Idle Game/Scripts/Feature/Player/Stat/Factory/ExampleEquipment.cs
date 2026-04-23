using System.Collections.Generic;

public static class ExampleEquipment
{
    public static EquipmentRuntimeData sword = new EquipmentRuntimeData()
    {
        ItemId = "iron_sword_01",
        Modifiers = new List<StatModifier>
        {
            StatModifierFactory.Add(StatType.AttackPower, 10f, ModifierLayer.Equipment, 0, null, "iron_sword_01")
        }
    };
}