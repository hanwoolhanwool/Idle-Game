public sealed class PlayerState_Dead : PlayerStateBase
{
    public override PlayerStateID StateID => PlayerStateID.Dead;

    public PlayerState_Dead(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
        
    }

    public override void Enter()
    {
        
    }

    public override void FixedTick(float deltaTime)
    {
        
    }

    public override void Exit()
    {
        
    }
}