public sealed class PlayerState_Dead : PlayerStateBase
{
    public override PlayerStateID StateID => PlayerStateID.Dead;

    public PlayerState_Dead(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
        
    }

    public override void Enter()
    {
        // 사망 처리기에서 이미 정지시키지만, 어떤 경로로 진입하든 이동이 멈추도록 방어.
        Context.SetDead(true);
        Context.PlayerMovementController?.SetMovementEnabled(false);
    }

    public override void FixedTick(float deltaTime)
    {

    }

    public override void Exit()
    {
        
    }
}