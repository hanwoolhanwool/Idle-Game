public sealed class PlayerState_Move : PlayerStateBase
{
    public override PlayerStateID StateID => PlayerStateID.Move;

    public PlayerState_Move(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
        
    }

    public override void Enter()
    {
        
    }

    public override void Tick(float deltaTime)
    {
        if (!Context.CanProcessInput)
            return;
        
        if(Context.PlayerMovementController.MoveInput.sqrMagnitude <= 0f)
            PlayerStateMachine.TryChangeState(PlayerStateID.Idle);
    }

    public override void Exit()
    {
        
    }
}