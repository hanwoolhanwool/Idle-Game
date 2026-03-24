using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStat", menuName = "Idle Game/Player Stat")]
[System.Serializable]
public class PlayerStat : ScriptableObject
{
    [Header("Movement Settings")]
    public float MaxHp;
    public float MaxMp;
    public float Attack;
    public float AttackSpeed;
    public float CritChance;
    public float CritDamage;
    public float MoveSpeed;
    [Header("Input")] 
    public float InputDeadZone;
    
    [Header("Sprite")]
    public bool useSpriteFlip;
}
