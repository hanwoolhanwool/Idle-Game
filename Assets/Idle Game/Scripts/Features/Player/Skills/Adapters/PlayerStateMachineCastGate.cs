public sealed class PlayerStateMachineCastGate : ICastGate
{
    private readonly PlayerStateMachine _stateMachine;
    private readonly PlayerStateID _castStateID;
    private readonly PlayerStateID _returnStateID;

    public PlayerStateMachineCastGate(
        PlayerStateMachine stateMachine,
        PlayerStateID castingSkill = PlayerStateID.Attack,
        PlayerStateID returnStateID = PlayerStateID.Idle
    )
    {
        _stateMachine = stateMachine;
        _castStateID = castingSkill;
        _returnStateID = returnStateID;
    }
    public bool IsCasting => _stateMachine.CurrentStateID == _castStateID;
    
    public void EnterCast() => _stateMachine.TryChangeState(_castStateID);

    public void ExitCast() => _stateMachine.TryChangeState(_returnStateID);
}