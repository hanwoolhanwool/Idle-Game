using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSkillController : ITickable
{
    private readonly SkillLoadout _loadout;
    private readonly SkillCooldownTracker _cooldownTracker;
    private readonly PlayerStatComponent _statComponent;
    private readonly PlayerCombatController _combatController;
    private readonly PlayerBuffController _buffController;
    private readonly Dictionary<SkillType, ISkillEffect> _effects;
    private readonly IPlayerMovementController _movementController;
    private readonly ICastGate _castGate;
    
    private SkillDefinition _castingSkill;
    private float _castRemaining;

    public bool IsCasting => _castGate.IsCasting;

    public PlayerSkillController(
        SkillLoadout loadout,
        SkillCooldownTracker cooldownTracker,
        PlayerCombatController combatController,
        PlayerStatComponent statComponent,
        PlayerBuffController buffController,
        IPlayerMovementController movementController,
        ICastGate  castGate
    )
    {
        _loadout = loadout;
        _cooldownTracker = cooldownTracker;
        _combatController = combatController;
        _statComponent = statComponent;
        _buffController = buffController;
        _movementController = movementController;
        _castGate = castGate;

        _effects = new Dictionary<SkillType, ISkillEffect>
        {
            { SkillType.Attack, new AttackSkillEffect() },
            { SkillType.Buff, new BuffSkillEffect() }
        };
    }

    public bool TryUseSkill(int slotIndex, IDamageable target = null)
    {
        SkillDefinition skill = _loadout.GetSlot(slotIndex);

        if (skill == null)
            return false;

        if (IsCasting)
            return false;
        
        if(!_cooldownTracker.IsReady(skill.SkillId))
            return false;
        
        if (!_effects.TryGetValue(skill.Type, out ISkillEffect effect))
            return false;

        if (!_statComponent.TrySpendMp(skill.ManaCost))
            return false;

        SkillContext context = new SkillContext(skill, _combatController, _buffController, target);
        effect.Execute(context);

        BeginCast(skill);
        return true;
    }

    private void BeginCast(SkillDefinition skill)
    {
        _castingSkill = skill;
        _castRemaining = skill.CastTime;
        
        if(!skill.CanMoveWhileCasting)
            _movementController.SetMovementEnabled(false);
                
        _castGate.EnterCast();
        
        if (_castRemaining <= 0f)
            EndCast();
    }

    private void EndCast()
    {
        if (_castingSkill == null)
            return;

        _movementController.SetMovementEnabled(true);   // 차후 특정 상태에 따른 이동 복원 추가
        
        float cooldown = ComputeEffectiveCooldown(_castingSkill);
        _cooldownTracker.StartCooldown(_castingSkill.SkillId, cooldown);
        
        _castingSkill = null;
        _castRemaining = 0f;
        
        _castGate.ExitCast();
    }

    private float ComputeEffectiveCooldown(SkillDefinition skill)
    {
        float reduction = _statComponent.Stats.GetFinal(StatType.CooldownReduction);
        reduction = Mathf.Clamp01(reduction);
        return skill.Cooldown * (1f - reduction);
    }

    public void Tick(float deltaTime)
    {
        _cooldownTracker.Tick(deltaTime);

        if (!IsCasting)
            return;
        _castRemaining -= deltaTime;
        if (_castRemaining <= 0f)
            EndCast();
    }
}