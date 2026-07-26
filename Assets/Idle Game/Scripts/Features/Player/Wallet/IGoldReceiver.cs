/// <summary>
/// 골드 수신 진입점(경계 계약). 보상 발행 측은 이 계약 하나만 알면 되며,
/// 재화 시스템의 구체 구현(<see cref="PlayerWallet"/>)을 몰라도 된다(ISP·DIP).
/// 경험치 수신(<c>IExpReceiver</c>)과 분리한 이유: 골드를 받는 주체(지갑)와
/// 경험치를 받는 주체(성장)는 서로 다른 객체이기 때문이다(ISP).
/// </summary>
public interface IGoldReceiver
{
    /// <summary>골드를 지급한다. 0 이하 처리는 구현 측 계약에 따른다.</summary>
    void AddGold(long amount);
}
