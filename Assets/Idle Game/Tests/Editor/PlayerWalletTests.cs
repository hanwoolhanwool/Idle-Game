using NUnit.Framework;

/// <summary>
/// 재화 지갑의 원자성·방어 로직 검증. (M0 계획서 §10 케이스 6)
/// </summary>
public sealed class PlayerWalletTests
{
    [Test]
    public void TrySpend_잔액이_부족하면_false를_반환하고_잔액을_건드리지_않는다()
    {
        var wallet = new PlayerWallet();
        wallet.AddGold(100);

        bool spent = wallet.TrySpend(150);

        Assert.IsFalse(spent);
        // 원자성이 깨지면 "구매는 실패했는데 돈은 빠져나간" 최악의 버그가 된다.
        Assert.AreEqual(100L, wallet.Gold);
    }

    [Test]
    public void TrySpend_잔액이_충분하면_차감하고_true를_반환한다()
    {
        var wallet = new PlayerWallet();
        wallet.AddGold(100);

        Assert.IsTrue(wallet.TrySpend(40));
        Assert.AreEqual(60L, wallet.Gold);
    }

    [Test]
    public void TrySpend_잔액과_정확히_같은_금액은_지불할_수_있다()
    {
        var wallet = new PlayerWallet();
        wallet.AddGold(100);

        // 경계값. 부등호를 잘못 쓰면 "전액 구매"가 영원히 불가능해진다.
        Assert.IsTrue(wallet.TrySpend(100));
        Assert.AreEqual(0L, wallet.Gold);
    }

    [Test]
    public void TrySpend_0이하는_거절한다()
    {
        var wallet = new PlayerWallet();
        wallet.AddGold(100);

        // 음수 지불을 허용하면 그것이 곧 무한 골드 획득 경로가 된다.
        Assert.IsFalse(wallet.TrySpend(0));
        Assert.IsFalse(wallet.TrySpend(-50));
        Assert.AreEqual(100L, wallet.Gold);
    }

    [Test]
    public void AddGold_0이하는_무시한다()
    {
        var wallet = new PlayerWallet();

        wallet.AddGold(0);
        wallet.AddGold(-100);

        Assert.AreEqual(0L, wallet.Gold);
    }

    [Test]
    public void AddGold_잔액_변경을_이벤트로_알린다()
    {
        var wallet = new PlayerWallet();
        long reported = -1;
        wallet.GoldChanged += g => reported = g;

        wallet.AddGold(250);

        // 골드는 스탯이 아니라서 StatMachine.OnStatChanged로는 표시 갱신이 걸리지 않는다.
        Assert.AreEqual(250L, reported);
    }

    [Test]
    public void 세이브_왕복_후_잔액이_유지된다()
    {
        var source = new PlayerWallet();
        source.AddGold(1_234_567);

        var data = new PlayerSaveData();
        source.CaptureState(data);

        var restored = new PlayerWallet();
        restored.RestoreState(data);

        Assert.AreEqual(source.Gold, restored.Gold);
    }

    [Test]
    public void RestoreState_재화_섹션이_비어도_0으로_시작한다()
    {
        var wallet = new PlayerWallet();
        wallet.AddGold(500);

        // 구버전 세이브에는 지갑 섹션이 아예 없을 수 있다.
        wallet.RestoreState(new PlayerSaveData { Wallet = null });

        Assert.AreEqual(0L, wallet.Gold);
    }
}
