using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player/Skill Definition", fileName = "SkillDefinition")]
public sealed class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    public string SkillId;
    public string DisplayName;
    public Sprite Icon;
    public SkillType Type;

    [Header("Cost & Timing")] 
    public float ManaCost;
    public float Cooldown;
    public float CastTime;

    [Header("Buff (Type == Buff)")] 
    public BuffDefinition LinkedBuff;

    [Header("Attack (Type == Attack)")] 
    public float DamageMultiplier = 1f;

    [Header("Csting Behavior")] 
    public bool CanMoveWhileCasting = false;
}