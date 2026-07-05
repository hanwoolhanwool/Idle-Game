public sealed class AutoCastController : ITickable
{
    private readonly PlayerSkillController _skillController;
    private readonly AutoBattleInputSource _autoBattle;

    public AutoCastController(
        PlayerSkillController skillController,
        AutoBattleInputSource autoBattle
    )
    {
        _skillController = skillController;
        _autoBattle = autoBattle;
    }

    public void Tick(float deltaTime)
    {
        if (_skillController == null || _autoBattle == null)
            return;
        if (!_autoBattle.InAttackRange)
            return;
        IDamageable target = _autoBattle.CurrentTarget != null
            ? _autoBattle.CurrentTarget.GetComponent<IDamageable>()
            : null;
        for (int slot = 1; slot < SkillLoadout.SlotCount; slot++)
        {
            if (_skillController.TryUseSkill(slot, target))
                return;
        }

        _skillController.TryUseSkill(0, target);
    }
}