using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player/Buff Definition", fileName = "BuffDefinition")]
public sealed class BuffDefinition : ScriptableObject
{
    public string BuffId;
    public float Duration = 5f;
    public bool RefreshDurationOnReapply = true;
    public RuntimeModifierEntry[] Modifiers;
}