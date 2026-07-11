/// <summary>
/// 경험치 수신 진입점(경계 계약). 보상 발행 측은 이 계약 하나만 알면 되며,
/// 성장 시스템의 구체 구현(<see cref="PlayerProgressionController"/>)을 몰라도 된다(ISP·DIP).
/// </summary>
public interface IExpReceiver
{
    /// <summary>경험치를 지급한다. 0 이하 처리는 구현/발행 측 계약에 따른다.</summary>
    void AddExp(int amount);
}
