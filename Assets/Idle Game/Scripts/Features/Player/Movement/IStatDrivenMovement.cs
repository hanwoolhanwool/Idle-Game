/// <summary>
/// 이동 속도 등 이동 수치를 런타임 스탯(<see cref="StatMachine"/>)에서 얻는 이동 컨트롤러 계약.
/// 상태머신용 계약(<see cref="IPlayerMovementController"/>)과 분리해,
/// 스탯 주입은 조립 루트(<c>PlayerRoot</c>)만 담당한다. (ISP)
/// </summary>
public interface IStatDrivenMovement
{
    /// <summary>이동에 사용할 읽기 전용 스탯 소스를 주입한다.</summary>
    void BindStats(IReadOnlyStats stats);
}
