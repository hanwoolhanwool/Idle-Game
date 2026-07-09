using UnityEngine;

/// <summary>
/// 6슬롯 스킬 편성을 하나의 에셋으로 분리한 데이터(§12). 편성=데이터, 조립=PlayerRoot로 역할을 가른다.
/// "전사용/마법사용" 프리셋을 여러 개 만들어 교체하거나, 세이브·런타임 편성 UI의 토대로 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "Idle Game/Skill Loadout Config", fileName = "SkillLoadoutConfig")]
public sealed class SkillLoadoutConfig : ScriptableObject
{
    [Header("Slot 0 — Basic Attack")]
    public SkillDefinition BasicAttack;

    [Header("Slots 1~5 — Equipped Skills")]
    public SkillDefinition[] EquippedSkills = new SkillDefinition[SkillLoadout.SlotCount - 1];
}
