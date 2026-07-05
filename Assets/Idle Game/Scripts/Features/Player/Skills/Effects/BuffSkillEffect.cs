public sealed class BuffSkillEffect : ISkillEffect
{
    public void Execute(SkillContext context)
    {
        if (context?.Definition == null || context.Buffs == null)
            return;

        BuffDefinition buff = context.Definition.LinkedBuff;
        if (buff == null)
            return;
        
        context.Buffs.Apply(buff);
    }
}