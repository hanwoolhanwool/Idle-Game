using UnityEngine;
public sealed class AttackSkillEffect : ISkillEffect
{
    public void Execute(SkillContext context)
    {
        if (context?.Definition == null || context.Combat == null)
            return;

        float damage = context.Combat.GetExpectedDamagePerHit() * context.Definition.DamageMultiplier;
        
        if (context.Target != null)
            context.Target.ApplyDamage(damage);
        else
            Debug.Log($"[AttackSkill] {context.Definition.DisplayName} → {damage} 데미지 (타겟 없음)");
    }
}