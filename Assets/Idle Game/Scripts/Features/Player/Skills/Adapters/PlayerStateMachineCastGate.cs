/// <summary>
/// 시전 중 상태 전이를 상태머신에 위임하는 어댑터.
/// 시전 상태(<see cref="PlayerStateID.Casting"/>)와 복귀 상태(<see cref="PlayerStateID.Idle"/>)는
/// 기본값 없이 호출부(PlayerRoot)가 반드시 명시한다 — 기본값이 실제 배선과 어긋난 채
/// 잠복하는 것을 컴파일 타임에 차단하기 위함이다.
/// </summary>
public sealed class PlayerStateMachineCastGate : ICastGate
{
    private readonly PlayerStateMachine _stateMachine;
    private readonly PlayerStateID _castStateID;
    private readonly PlayerStateID _returnStateID;

    public PlayerStateMachineCastGate(
        PlayerStateMachine stateMachine,
        PlayerStateID castStateID,
        PlayerStateID returnStateID
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
