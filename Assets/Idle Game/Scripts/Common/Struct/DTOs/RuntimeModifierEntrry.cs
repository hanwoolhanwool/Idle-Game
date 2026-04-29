using System;

[Serializable]
public struct RuntimeModifierEntry
{
    public StatType StatType;
    public ModifierOp Operation;
    public ModifierLayer Layer;
    public float Value;
    public int Order;
}