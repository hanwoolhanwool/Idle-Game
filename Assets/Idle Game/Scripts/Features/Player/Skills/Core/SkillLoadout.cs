public sealed class SkillLoadout
{
    public const int SlotCount = 6;
    private const int BasicAttackSlot = 0;
    
    private readonly SkillDefinition[] _slots = new SkillDefinition[SlotCount];

    public SkillLoadout(SkillDefinition basicAttack)
    {
        _slots[BasicAttackSlot] = basicAttack;
    }

    public SkillDefinition GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return null;
        
        return _slots[index];
    }

    public bool TryEquip(int index, SkillDefinition skill)
    {
        if (index <= BasicAttackSlot || index >= SlotCount)
            return false;

        _slots[index] = skill;
        return true;
    }
}