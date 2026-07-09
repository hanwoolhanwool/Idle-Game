/// <summary>
/// 시전 중 상태 전이를 상태머신에 위임하는 어댑터.
/// 현재는 시전 상태로 <see cref="PlayerStateID.Attack"/>을 재사용한다("평타"와 "스킬 시전"이 같은 상태).
/// 시전 중 애니메이션/피격 처리를 세분화할 때는 전용 <c>Casting</c> 상태 분리를 검토한다(§3-D).
/// 재사용 여부를 호출부(PlayerRoot)에서 명시적으로 주입하도록 파라미터로 노출한다.
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
