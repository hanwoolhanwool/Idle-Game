/// <summary>
/// 플레이어의 골드 잔액을 소유하는 순수 C# 재화 지갑. 금액은 인플레이션 대비 <see cref="long"/>.
/// MonoBehaviour 비종속이라 세이브·서버 이식·단위 테스트가 쉽다(DIP).
/// </summary>
public sealed class PlayerWallet : IGoldReceiver
{
    public long Gold { get; private set; }

    /// <summary>골드를 더한다. 0 이하는 무시한다(오지급·음수 방어).</summary>
    public void AddGold(long amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
    }

    /// <summary>
    /// 잔액이 충분하면 차감하고 true를 반환한다. 부족하면 잔액을 <b>건드리지 않고</b> false(원자성).
    /// </summary>
    public bool TrySpend(long amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }
}
