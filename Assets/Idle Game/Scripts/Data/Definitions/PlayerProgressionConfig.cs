using UnityEngine;

/// <summary>
/// 플레이어의 <b>시작 상태</b>(신규 게임 진입점)를 담는 설정.
///
/// <para>
/// 레벨별 베이스 스탯은 여기 있지 않다 — 그것은 <see cref="PlayerLevelTable"/>의 책임이다(SRP).
/// 이전에는 이 SO가 시작 스탯(StartMaxHp 등)을 직접 들고 있었고 리졸버가 레벨을 무시한 채
/// 그 값을 그대로 반환했기 때문에, 레벨이 올라도 스탯이 전혀 변하지 않았다.
/// "시작 상태"와 "성장 규칙"을 분리해 스탯의 진실 공급원을 하나로 만든다.
/// </para>
/// </summary>
[CreateAssetMenu(menuName = "Game/Player/Progression Config", fileName = "PlayerProgressionConfig")]
public sealed class PlayerProgressionConfig : ScriptableObject
{
    [Header("Stored State (신규 게임 시작값)")]
    [Min(1)]
    public int StartLevel = 1;

    [Min(0)]
    public int StartExp = 0;

    [Min(0)]
    [Tooltip("전직 차수. 이 값을 올리는 주체는 아직 없다(전직 시스템은 M2).")]
    public int PromotionTier = 0;
}
