public abstract class PlayerStateBase : IPlayerState
{
    protected readonly PlayerStateMachine PlayerStateMachine;
    protected readonly PlayerStateContext Context;
    public abstract PlayerStateID StateID { get; }

    protected PlayerStateBase(PlayerStateMachine stateMachine)
    {
        PlayerStateMachine = stateMachine;
        Context = stateMachine.Context;
    }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void FixedTick(float fixedDeltaTime) { }
}