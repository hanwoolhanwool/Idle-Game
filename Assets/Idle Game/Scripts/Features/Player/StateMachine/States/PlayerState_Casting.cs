/// <summary>
/// 스킬 시전 중 상태. "평타(Attack)"와 "스킬 시전"을 분리해, 시전 중 애니메이션·피격
/// 처리를 평타와 구분할 수 있게 한다(§3-D). 시전 진입/종료는 <see cref="PlayerSkillController"/>가
/// <see cref="ICastGate"/>를 통해 구동하므로, 이 상태 자체는 표식 역할만 한다.
/// </summary>
public sealed class PlayerState_Casting : PlayerStateBase
{
    public override PlayerStateID StateID => PlayerStateID.Casting;

    public PlayerState_Casting(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }
}
