using System;
using NUnit.Framework;

/// <summary>
/// 오프라인 보상 환산·지급 검증. (M1 계획서 §10 케이스 9~11)
/// <para>
/// 계산기가 시간을 <b>인자로</b> 받는 설계라 이 테스트들이 가능하다. 내부에서
/// <c>DateTime.UtcNow</c>를 읽었다면 "8시간 비웠을 때"를 검증할 방법이 없다.
/// </para>
/// </summary>
public sealed class OfflineRewardTests
{
    private StageDefinition _stage;
    private OfflineRewardConfig _config;

    [SetUp]
    public void SetUp()
    {
        // 계산을 암산으로 따라갈 수 있게 보상을 1로 둔다(추정 처치 수 = 경험치).
        _stage = TestFactory.CreateStage("stage_01", killsToClear: 1000, expReward: 1, goldReward: 1);
        _config = TestFactory.CreateOfflineConfig(maxHours: 8f, efficiency: 1f);
    }

    [TearDown]
    public void TearDown()
    {
        TestFactory.DestroyStage(_stage);
        TestFactory.Destroy(_config);
    }

    // ───────────────────────── 케이스 9: 비례와 상한 ─────────────────────────

    [Test]
    public void 경과_시간에_비례해_보상이_늘어난다()
    {
        // 1마리/초 × 3600초 × 효율 1 = 3600
        OfflineReward oneHour = OfflineRewardCalculator.Calculate(
            TimeSpan.FromHours(1), killsPerSecond: 1f, _stage, _config);

        OfflineReward twoHours = OfflineRewardCalculator.Calculate(
            TimeSpan.FromHours(2), killsPerSecond: 1f, _stage, _config);

        Assert.AreEqual(3600, oneHour.Exp);
        Assert.AreEqual(7200, twoHours.Exp);
    }

    [Test]
    public void 상한을_넘는_시간은_상한에서_멈춘다()
    {
        OfflineReward atLimit = OfflineRewardCalculator.Calculate(
            TimeSpan.FromHours(8), killsPerSecond: 1f, _stage, _config);

        OfflineReward wayBeyond = OfflineRewardCalculator.Calculate(
            TimeSpan.FromDays(30), killsPerSecond: 1f, _stage, _config);

        // 상한이 없으면 한 달 방치가 최종 콘텐츠를 통째로 건너뛴다.
        Assert.AreEqual(atLimit.Exp, wayBeyond.Exp);
        Assert.AreEqual(8 * 3600, atLimit.Exp);
    }

    [Test]
    public void 처치율에_비례해_보상이_늘어난다()
    {
        OfflineReward slow = OfflineRewardCalculator.Calculate(
            TimeSpan.FromHours(1), killsPerSecond: 0.5f, _stage, _config);

        OfflineReward fast = OfflineRewardCalculator.Calculate(
            TimeSpan.FromHours(1), killsPerSecond: 2f, _stage, _config);

        Assert.AreEqual(1800, slow.Exp);
        Assert.AreEqual(7200, fast.Exp);
    }

    [Test]
    public void 스테이지_보상값이_처치수에_곱해진다()
    {
        StageDefinition rich = TestFactory.CreateStage("rich", expReward: 10, goldReward: 7);
        try
        {
            OfflineReward reward = OfflineRewardCalculator.Calculate(
                TimeSpan.FromHours(1), killsPerSecond: 1f, rich, _config);

            Assert.AreEqual(3600, reward.Kills);
            Assert.AreEqual(36000, reward.Exp);
            Assert.AreEqual(25200L, reward.Gold);
        }
        finally
        {
            TestFactory.DestroyStage(rich);
        }
    }

    // ───────────────────────── 케이스 10: 지급하지 않는 경우 ─────────────────────────

    [Test]
    public void 경과가_0이하면_보상이_없다()
    {
        // 시계를 과거로 되돌린 경우. 예외 대신 0으로 흘린다.
        Assert.IsFalse(OfflineRewardCalculator
            .Calculate(TimeSpan.Zero, 1f, _stage, _config).HasReward);

        Assert.IsFalse(OfflineRewardCalculator
            .Calculate(TimeSpan.FromHours(-5), 1f, _stage, _config).HasReward);
    }

    [Test]
    public void 처치율이_0이면_보상이_없다()
    {
        // 한 번도 싸우지 않고 끈 세션. 환산할 성과가 없다.
        Assert.IsFalse(OfflineRewardCalculator
            .Calculate(TimeSpan.FromHours(8), 0f, _stage, _config).HasReward);
    }

    [Test]
    public void 스테이지나_설정이_없으면_보상이_없다()
    {
        Assert.IsFalse(OfflineRewardCalculator
            .Calculate(TimeSpan.FromHours(1), 1f, null, _config).HasReward);

        Assert.IsFalse(OfflineRewardCalculator
            .Calculate(TimeSpan.FromHours(1), 1f, _stage, null).HasReward);
    }

    [Test]
    public void 한_마리에_못_미치는_짧은_이탈은_보상이_없다()
    {
        // 1초에 0.01마리 × 10초 = 0.1마리. 반올림해 1을 주면 앱을 껐다 켜는 것만으로 이득이 생긴다.
        Assert.IsFalse(OfflineRewardCalculator
            .Calculate(TimeSpan.FromSeconds(10), 0.01f, _stage, _config).HasReward);
    }

    // ───────────────────────── 케이스 11: 효율 계수 ─────────────────────────

    [Test]
    public void 효율_계수가_결과에_곱해진다()
    {
        OfflineRewardConfig half = TestFactory.CreateOfflineConfig(maxHours: 8f, efficiency: 0.5f);
        try
        {
            OfflineReward full = OfflineRewardCalculator.Calculate(
                TimeSpan.FromHours(1), 1f, _stage, _config);

            OfflineReward halved = OfflineRewardCalculator.Calculate(
                TimeSpan.FromHours(1), 1f, _stage, half);

            // 효율이 1이면 접속해 있을 이유가 사라진다.
            Assert.AreEqual(full.Exp / 2, halved.Exp);
        }
        finally
        {
            TestFactory.Destroy(half);
        }
    }

    [Test]
    public void 효율이_0이면_보상이_없다()
    {
        OfflineRewardConfig disabled = TestFactory.CreateOfflineConfig(maxHours: 8f, efficiency: 0f);
        try
        {
            Assert.IsFalse(OfflineRewardCalculator
                .Calculate(TimeSpan.FromHours(8), 1f, _stage, disabled).HasReward);
        }
        finally
        {
            TestFactory.Destroy(disabled);
        }
    }

    // ───────────────────────── 컨트롤러 통합 ─────────────────────────

    [Test]
    public void 복원_시_경과분이_기존_보상_경로로_지급된다()
    {
        var catalog = TestFactory.CreateCatalog(_stage);
        var receiver = new SpyRewardReceiver();
        var savedAt = new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc);

        try
        {
            var data = new PlayerSaveData();
            data.World.StageId = "stage_01";
            data.World.KillsPerSecond = 1f;
            data.World.LastSaveUtcTicks = savedAt.Ticks;

            // 2시간 뒤 복귀
            var loader = new StageController(
                catalog, null, receiver, receiver, _config, () => savedAt.AddHours(2));
            loader.Initialize();
            loader.RestoreState(data);

            Assert.AreEqual(7200, receiver.TotalExp);
            Assert.AreEqual(7200L, receiver.TotalGold);
        }
        finally
        {
            TestFactory.Destroy(catalog);
        }
    }

    [Test]
    public void 오프라인_처치는_클리어_카운트를_올리지_않는다()
    {
        var catalog = TestFactory.CreateCatalog(_stage);
        var receiver = new SpyRewardReceiver();
        var savedAt = new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc);

        try
        {
            var data = new PlayerSaveData();
            data.World.StageId = "stage_01";
            data.World.KillsInStage = 2;
            data.World.KillsPerSecond = 1f;
            data.World.LastSaveUtcTicks = savedAt.Ticks;

            var loader = new StageController(
                catalog, null, receiver, receiver, _config, () => savedAt.AddHours(8));
            loader.Initialize();
            loader.RestoreState(data);

            // 오프라인 전환을 허용하면 방치만으로 최종 스테이지에 도달할 수 있어,
            // "도중에 최소 한 번 벽에 막힌다"는 M1 목표와 충돌한다.
            Assert.AreEqual(2, loader.KillsInStage);
            Assert.AreSame(_stage, loader.CurrentStage);
        }
        finally
        {
            TestFactory.Destroy(catalog);
        }
    }

    [Test]
    public void 첫_실행이면_오프라인_보상이_없다()
    {
        var catalog = TestFactory.CreateCatalog(_stage);
        var receiver = new SpyRewardReceiver();

        try
        {
            var data = new PlayerSaveData();
            data.World.StageId = "stage_01";
            data.World.KillsPerSecond = 1f;
            data.World.LastSaveUtcTicks = 0L; // 기준선 없음

            var loader = new StageController(
                catalog, null, receiver, receiver, _config, () => DateTime.UtcNow);
            loader.Initialize();
            loader.RestoreState(data);

            Assert.AreEqual(0, receiver.TotalExp);
        }
        finally
        {
            TestFactory.Destroy(catalog);
        }
    }

    [Test]
    public void 손상된_저장_시각은_예외없이_무시된다()
    {
        var catalog = TestFactory.CreateCatalog(_stage);
        var receiver = new SpyRewardReceiver();

        try
        {
            var data = new PlayerSaveData();
            data.World.StageId = "stage_01";
            data.World.KillsPerSecond = 1f;
            data.World.LastSaveUtcTicks = long.MaxValue; // DateTime 범위를 벗어난다

            var loader = new StageController(
                catalog, null, receiver, receiver, _config, () => DateTime.UtcNow);
            loader.Initialize();

            // new DateTime(ticks)가 던지면 로드 전체가 실패해 진행을 잃는다.
            Assert.DoesNotThrow(() => loader.RestoreState(data));
            Assert.AreEqual(0, receiver.TotalExp);
        }
        finally
        {
            TestFactory.Destroy(catalog);
        }
    }

    [Test]
    public void 처치율은_틱_구간_평균으로_측정된다()
    {
        var catalog = TestFactory.CreateCatalog(_stage);
        var receiver = new SpyRewardReceiver();

        try
        {
            var controller = new StageController(catalog, null, receiver, receiver, _config);
            controller.Initialize();

            // 5초 창에 10마리 → 2마리/초
            for (int i = 0; i < 10; i++)
                controller.HandleKill();
            controller.Tick(5f);

            Assert.AreEqual(2f, controller.KillsPerSecond, 0.01f);
        }
        finally
        {
            TestFactory.Destroy(catalog);
        }
    }

    [Test]
    public void 샘플_구간_전에는_처치율이_갱신되지_않는다()
    {
        var catalog = TestFactory.CreateCatalog(_stage);
        var receiver = new SpyRewardReceiver();

        try
        {
            var controller = new StageController(catalog, null, receiver, receiver, _config);
            controller.Initialize();

            controller.HandleKill();
            controller.Tick(1f);

            // 구간이 짧으면 전투 공백 한 번에 값이 크게 흔들린다.
            Assert.AreEqual(0f, controller.KillsPerSecond, 0.001f);
        }
        finally
        {
            TestFactory.Destroy(catalog);
        }
    }

    private sealed class SpyRewardReceiver : IExpReceiver, IGoldReceiver
    {
        public int TotalExp;
        public long TotalGold;

        public void AddExp(int amount) => TotalExp += amount;
        public void AddGold(long amount) => TotalGold += amount;
    }
}
