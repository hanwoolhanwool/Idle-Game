public sealed class PlayerState_Idle : PlayerStateBase
{
    public override PlayerStateID StateID => PlayerStateID.Idle;

    public PlayerState_Idle(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
        
    }

    public override void Enter()
    {
        
    }

    public override void Tick(float deltaTime)
    {
        if (!Context.CanProcessInput)
            return;
        
        if(Context.PlayerMovementController.MoveInput.sqrMagnitude > 0f)
            PlayerStateMachine.TryChangeState(PlayerStateID.Move);
    }

    public override void Exit()
    {
        
    }
}