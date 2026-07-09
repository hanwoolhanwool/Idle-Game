/// <summary>
/// 시전 중 상태 전이를 상태머신에 위임하는 어댑터.
/// 시전 상태(<see cref="PlayerStateID.Casting"/>)와 복귀 상태(<see cref="PlayerStateID.Idle"/>)를
/// 호출부(PlayerRoot)에서 명시적으로 주입한다. 평타(Attack)와 스킬 시전을 구분한다(§3-D).
/// </summary>
public sealed class PlayerStateMachineCastGate : ICastGate
{
    private readonly PlayerStateMachine _stateMachine;
    private readonly PlayerStateID _castStateID;
    private readonly PlayerStateID _returnStateID;

    public PlayerStateMachineCastGate(
        PlayerStateMachine stateMachine,
        PlayerStateID castStateID = PlayerStateID.Attack,
        PlayerStateID returnStateID = PlayerStateID.Idle
    )
    {
        _stateMachine = stateMachine;
        _castStateID = castStateID;
        _returnStateID = returnStateID;
    }

    public bool IsCasting => _stateMachine.CurrentStateID == _castStateID;

    public void EnterCast() => _stateMachine.TryChangeState(_castStateID);

    public void ExitCast() => _stateMachine.TryChangeState(_returnStateID);
}
