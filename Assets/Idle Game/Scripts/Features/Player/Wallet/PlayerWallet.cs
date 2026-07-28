using System;

/// <summary>
/// 플레이어의 골드 잔액을 소유하는 순수 C# 재화 지갑. 금액은 인플레이션 대비 <see cref="long"/>.
/// MonoBehaviour 비종속이라 세이브·서버 이식·단위 테스트가 쉽다(DIP).
/// </summary>
public sealed class PlayerWallet : IGoldReceiver, ISaveable
{
    /// <summary>골드의 통화 식별자. 재화가 늘면 엔트리만 추가된다(정본 §12).</summary>
    public const string GoldCurrencyId = "gold";

    public long Gold { get; private set; }

    /// <summary>
    /// 잔액이 바뀐 직후 새 잔액과 함께 발행된다. 지갑은 구독자(HUD·업적·상점)를 알지 못한다(DIP).
    /// 골드는 스탯이 아니라 <c>StatMachine.OnStatChanged</c>로는 표시 갱신을 트리거할 수 없어,
    /// 재화 축이 스스로 변경을 방송해야 한다.
    /// </summary>
    public event Action<long> GoldChanged;

    /// <summary>골드를 더한다. 0 이하는 무시한다(오지급·음수 방어).</summary>
    public void AddGold(long amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }

    /// <summary>
    /// 잔액이 충분하면 차감하고 true를 반환한다. 부족하면 잔액을 <b>건드리지 않고</b> false(원자성).
    /// </summary>
    public bool TrySpend(long amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;

        Gold -= amount;
        GoldChanged?.Invoke(Gold);
        return true;
    }

    public void CaptureState(PlayerSaveData data)
    {
        data.Wallet.SetAmount(GoldCurrencyId, Gold);
    }

    public void RestoreState(PlayerSaveData data)
    {
        // 섹션이 비어 있으면(구버전·신규 세이브) 0으로 시작한다.
        Gold = data.Wallet != null ? data.Wallet.GetAmount(GoldCurrencyId) : 0L;

        // 복원도 잔액 변경이다. 로드 직후 HUD가 저장된 골드를 곧바로 표시하려면 여기서도 알려야 한다.
        GoldChanged?.Invoke(Gold);
    }
}
