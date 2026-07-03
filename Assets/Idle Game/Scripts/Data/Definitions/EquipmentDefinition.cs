using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Equipment Definition")]
public sealed class EquipmentDefinition : ScriptableObject
{
    public string ItemId;
    public RuntimeModifierEntry[] Modifiers;
}