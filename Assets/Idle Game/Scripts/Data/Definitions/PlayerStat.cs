using UnityEngine;

/// <summary>
/// 이동 컨트롤러의 순수 설정 값(입력·표현). 전투 수치(MaxHp/Attack/MoveSpeed 등)는
/// 단일 출처인 StatMachine이 소유하므로 여기에 두지 않는다. (§3-A SoT)
/// </summary>
[CreateAssetMenu(fileName = "PlayerStat", menuName = "Idle Game/Player Stat")]
public class PlayerStat : ScriptableObject
{
    [Header("Input")]
    public float InputDeadZone;

    [Header("Sprite")]
    public bool useSpriteFlip;
}
