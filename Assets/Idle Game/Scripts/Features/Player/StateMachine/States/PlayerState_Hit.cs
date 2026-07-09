public sealed class PlayerState_Hit : PlayerStateBase
{
    // 피격 경직 시간(초). 게임 필(feel) 튜닝 대상.
    private const float StunDuration = 0.15f;

    private float _remaining;

    public override PlayerStateID StateID => PlayerStateID.Hit;

    public PlayerState_Hit(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        _remaining = StunDuration;
        Context.SetStunned(true);
        Context.PlayerMovementController?.SetMovementEnabled(false);
    }

    public override void Tick(float deltaTime)
    {
        _remaining -= deltaTime;
        if (_remaining <= 0f)
            PlayerStateMachine.TryChangeState(PlayerStateID.Idle);
    }

    public override void Exit()
    {
        Context.SetStunned(false);
        Context.PlayerMovementController?.SetMovementEnabled(true);
    }
}
