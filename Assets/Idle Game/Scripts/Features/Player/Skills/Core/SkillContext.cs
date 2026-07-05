public sealed class SkillContext
{
    public SkillDefinition Definition { get; }
    public PlayerCombatController Combat { get; }
    public PlayerBuffController Buffs { get; }
    public IDamageable Target { get; }
    
    public SkillContext(
        SkillDefinition definition,
        PlayerCombatController combat,
        PlayerBuffController buffs,
        IDamageable target = null)
    {
        Definition = definition;
        Combat = combat;
        Buffs = buffs;
        Target = target;
    }
}