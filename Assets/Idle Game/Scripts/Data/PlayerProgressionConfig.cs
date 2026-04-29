using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player/Progression Config", fileName = "PlayerProgressionConfig")]
public sealed class PlayerProgressionConfig : ScriptableObject
{
    [Header("Stored State")] 
    public int StartLevel = 1;
    public int StartExp = 0;
    public int PromotionTier = 0;
    
    [Header("Resolved Base Stats At Start")]
    public float StartMaxHp = 100f;
    public float StartAttackPower = 10f;
    public float StartAttackSpeed = 1f;
    public float StartMoveSpeed = 5f;
    public float StartDefense = 0f;
    public float StartMaxMana = 50f;
    public float StartHpRegen = 1f;
    public float StartManaRegen = 1f;
}