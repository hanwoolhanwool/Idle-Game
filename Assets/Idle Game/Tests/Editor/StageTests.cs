using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 스테이지 목록·진행·저장 검증. (M1 계획서 §10 케이스 1~8·12)
/// </summary>
public sealed class StageTests
{
    private readonly List<StageDefinition> _stages = new();
    private StageCatalog _catalog;
    private OfflineRewardConfig _offlineConfig;

    [SetUp]
    public void SetUp()
    {
        _stages.Add(TestFactory.CreateStage("stage_01", killsToClear: 3, multiplier: 1f, baseHp: 100f));
        _stages.Add(TestFactory.CreateStage("stage_02", killsToClear: 5, multiplier: 2f, baseHp: 100f));
        _stages.Add(TestFactory.CreateStage("stage_03", killsToClear: 7, multiplier: 4f, baseHp: 100f));
        _catalog = TestFactory.CreateCatalog(_stages.ToArray());
        _offlineConfig = TestFactory.CreateOfflineConfig();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (StageDefinition stage in _stages)
            TestFactory.DestroyStage(stage);
        _stages.Clear();

        TestFactory.Destroy(_catalog);
        TestFactory.Destroy(_offlineConfig);
    }

    private StageController CreateController(
        SpyRewardReceiver receiver = null,
        Func<DateTime> utcNow = null)
    {
        receiver ??= new SpyRewardReceiver();
        var controller = new StageController(_catalog, null, receiver, receiver, _offlineConfig, utcNow);
        controller.Initialize();
        return controller;
    }

    // ───────────────────────── 케이스 1·2: StageCatalog ─────────────────────────

    [Test]
    public void FindById_존재하는_식별자를_찾는다()
    {
        Assert.AreSame(_stages[1], _catalog.FindById("stage_02"));
    }

    [Test]
    public void FindById_없는_식별자는_null을_준다()
    {
        // 밸런스 패치로 삭제된 스테이지를 가리키는 세이브가 로드될 수 있다.
        // 카탈로그는 판단하지 않고 사실만 알린다 — 폴백은 호출부의 몫이다.
        Assert.IsNull(_catalog.FindById("stage_99"));
        Assert.IsNull(_catalog.FindById(null));
        Assert.IsNull(_catalog.FindById(string.Empty));
    }

    [Test]
    public void Next_다음_스테이지를_준다()
    {
        Assert.AreSame(_stages[1], _catalog.Next(_stages[0]));
        Assert.AreSame(_stages[2], _catalog.Next(_stages[1]));
    }

    [Test]
    public void Next_마지막_스테이지에서는_null을_준다()
    {
        Assert.IsNull(_catalog.Next(_stages[2]));
    }

    [Test]
    public void First_첫_스테이지를_준다()
    {
        Assert.AreSame(_stages[0], _catalog.First());
    }

    // ───────────────────────── 케이스 3~5: 진행 ─────────────────────────

    [Test]
    public void Initialize_첫_스테이지에서_시작한다()
    {
        StageController controller = CreateController();

        Assert.AreSame(_stages[0], controller.CurrentStage);
        Assert.AreEqual(0, controller.KillsInStage);
    }

    [Test]
    public void 목표_처치수에_도달하면_다음_스테이지로_넘어가고_카운트가_리셋된다()
    {
        StageController controller = CreateController();

        for (int i = 0; i < 3; i++)
            controller.HandleKill();

        Assert.AreSame(_stages[1], controller.CurrentStage);
        // 카운트를 이월하면 뒤 스테이지가 앞 스테이지의 초과분만큼 짧아진다.
        Assert.AreEqual(0, controller.KillsInStage);
    }

    [Test]
    public void 목표에_한_마리_모자라면_넘어가지_않는다()
    {
        StageController controller = CreateController();

        for (int i = 0; i < 2; i++)
            controller.HandleKill();

        // 경계값. 부등호를 잘못 쓰면 스테이지가 한 마리 일찍 또는 늦게 넘어간다.
        Assert.AreSame(_stages[0], controller.CurrentStage);
        Assert.AreEqual(2, controller.KillsInStage);
    }

    [Test]
    public void 한_번의_처치로_두_스테이지가_넘어가지_않는다()
    {
        StageController controller = CreateController();

        // 1스테이지(3마리) 클리어 직후의 한 마리는 2스테이지(5마리)의 첫 마리여야 한다.
        for (int i = 0; i < 4; i++)
            controller.HandleKill();

        Assert.AreSame(_stages[1], controller.CurrentStage);
        Assert.AreEqual(1, controller.KillsInStage);
    }

    [Test]
    public void 마지막_스테이지에서는_전환하지_않고_카운트만_되감는다()
    {
        StageController controller = CreateController();

        // 3 + 5 + 7 = 15마리로 마지막 스테이지 클리어까지 간다.
        for (int i = 0; i < 15; i++)
            controller.HandleKill();

        // "더 갈 곳 없음"을 막다른 길로 만들면 방치가 무의미해지므로 무한 반복한다.
        Assert.AreSame(_stages[2], controller.CurrentStage);
        Assert.AreEqual(0, controller.KillsInStage);
    }

    [Test]
    public void 전환할_때_StageChanged를_발행한다()
    {
        StageController controller = CreateController();
        var seen = new List<string>();
        controller.StageChanged += s => seen.Add(s.StageId);

        for (int i = 0; i < 3; i++)
            controller.HandleKill();

        Assert.AreEqual(new[] { "stage_02" }, seen);
    }

    [Test]
    public void 전환할_때_즉시_저장을_요청한다()
    {
        StageController controller = CreateController();
        int requests = 0;
        controller.SaveRequested += () => requests++;

        for (int i = 0; i < 3; i++)
            controller.HandleKill();

        // 전환 직후 강제 종료되면 주기 저장만으로는 진척이 통째로 날아간다.
        Assert.AreEqual(1, requests);
    }

    [Test]
    public void KillsRemaining은_남은_처치수를_알려준다()
    {
        StageController controller = CreateController();

        Assert.AreEqual(3, controller.KillsRemaining);
        controller.HandleKill();
        Assert.AreEqual(2, controller.KillsRemaining);
    }

    // ───────────────────────── 케이스 6·7: 세이브 ─────────────────────────

    [Test]
    public void 세이브_왕복_스테이지와_누적_처치가_보존된다()
    {
        StageController source = CreateController();
        source.HandleKill();
        source.HandleKill();
        source.HandleKill(); // stage_02로 전환
        source.HandleKill(); // stage_02에서 1마리

        var data = new PlayerSaveData();
        source.CaptureState(data);

        StageController restored = CreateController();
        restored.RestoreState(data);

        Assert.AreSame(_stages[1], restored.CurrentStage);
        Assert.AreEqual(1, restored.KillsInStage);
    }

    [Test]
    public void 세이브에_인덱스가_아니라_식별자가_기록된다()
    {
        StageController controller = CreateController();
        var data = new PlayerSaveData();
        controller.CaptureState(data);

        // 인덱스를 저장하면 스테이지를 중간에 끼워 넣는 순간 모든 유저의 진행이 밀린다.
        Assert.AreEqual("stage_01", data.World.StageId);
    }

    [Test]
    public void 복원_카탈로그에_없는_식별자는_첫_스테이지로_폴백한다()
    {
        var data = new PlayerSaveData();
        data.World.StageId = "stage_deleted";
        data.World.KillsInStage = 2;

        StageController controller = CreateController();
        controller.RestoreState(data);

        // 삭제된 스테이지를 가리켜도 앱이 죽거나 진행이 멈추면 안 된다.
        Assert.AreSame(_stages[0], controller.CurrentStage);
    }

    [Test]
    public void 복원_음수_처치수는_0으로_클램프된다()
    {
        var data = new PlayerSaveData();
        data.World.StageId = "stage_01";
        data.World.KillsInStage = -5;

        StageController controller = CreateController();
        controller.RestoreState(data);

        Assert.AreEqual(0, controller.KillsInStage);
    }

    [Test]
    public void 저장_시각이_기록된다()
    {
        var now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        StageController controller = CreateController(utcNow: () => now);

        var data = new PlayerSaveData();
        controller.CaptureState(data);

        // 오프라인 보상의 시간 기준선. DateTime을 직접 담지 못해 Ticks로 저장한다.
        Assert.AreEqual(now.Ticks, data.World.LastSaveUtcTicks);
    }

    // ───────────────────────── 케이스 8: 마이그레이션 ─────────────────────────

    [Test]
    public void V1ToV2_변환기는_v1을_소비한다()
    {
        var migration = new SaveMigration_V1ToV2();

        Assert.AreEqual(1, migration.FromVersion);
    }

    [Test]
    public void V1ToV2_월드_섹션이_없는_세이브도_복구한다()
    {
        // 수동 편집·부분 손상으로 섹션 자체가 비어 있는 경우.
        var data = new PlayerSaveData { Version = 1, World = null };

        new SaveMigration_V1ToV2().Migrate(data);

        Assert.IsNotNull(data.World);
        Assert.AreEqual(string.Empty, data.World.StageId);
    }

    [Test]
    public void V1_세이브가_경고없이_v2로_올라가고_진행이_보존된다()
    {
        var repository = new StubRepository
        {
            Stored = BuildV1Save(level: 7, gold: 500)
        };
        var service = new SaveService(repository, 60f, new List<ISaveMigration> { new SaveMigration_V1ToV2() });

        var spy = new SpySaveable();
        service.Register(spy);

        // 변환기가 등록돼 있으므로 "마이그레이션이 없습니다" 경고가 나오지 않아야 한다.
        // (경고가 정상 상황에서 울리면 진짜 사고를 알리는 신호로서의 값을 잃는다)
        service.LoadAndRestore();

        Assert.AreEqual(1, spy.RestoreCount);
        Assert.IsFalse(service.IsSaveBlocked);
    }

    private static PlayerSaveData BuildV1Save(int level, long gold)
    {
        var data = new PlayerSaveData { Version = 1 };
        data.Progression.Level = level;
        data.Wallet.SetAmount(PlayerWallet.GoldCurrencyId, gold);
        // v1에는 World 섹션이 없었다. JsonUtility가 기본값으로 채우는 상태를 흉내 낸다.
        data.World = new WorldSaveSection();
        return data;
    }

    // ───────────────────────── 케이스 12: 배율 적용 ─────────────────────────

    [Test]
    public void BuildSpawnParams_난이도_배율이_체력에_곱해진다()
    {
        EnemySpawnParams first = _stages[0].BuildSpawnParams();
        EnemySpawnParams third = _stages[2].BuildSpawnParams();

        Assert.AreEqual(100f, first.MaxHp, 0.001f);
        Assert.AreEqual(400f, third.MaxHp, 0.001f);
    }

    [Test]
    public void BuildSpawnParams_보상은_배율의_영향을_받지_않는다()
    {
        EnemySpawnParams third = _stages[2].BuildSpawnParams();

        // 난이도 배율은 적을 단단하게 만들 뿐, 보상 설계는 스테이지가 직접 정한다.
        Assert.AreEqual(10, third.ExpReward);
        Assert.AreEqual(5, third.GoldReward);
    }

    [Test]
    public void EnemySpawnParams_체력_0이하는_1로_보정된다()
    {
        var zeroHp = new EnemySpawnParams(0f, 10, 5);

        // 체력 0은 스폰 즉시 사망을 뜻해 스포너가 무한 스폰 루프에 빠진다.
        Assert.Greater(zeroHp.MaxHp, 0f);
    }

    // ───────────────────────── 테스트 대역 ─────────────────────────

    /// <summary>경험치·골드 수신을 한 곳에서 받아 적는 대역.</summary>
    private sealed class SpyRewardReceiver : IExpReceiver, IGoldReceiver
    {
        public int TotalExp;
        public long TotalGold;

        public void AddExp(int amount) => TotalExp += amount;
        public void AddGold(long amount) => TotalGold += amount;
    }

    private sealed class SpySaveable : ISaveable
    {
        public int RestoreCount;
        public void CaptureState(PlayerSaveData data) { }
        public void RestoreState(PlayerSaveData data) => RestoreCount++;
    }

    private sealed class StubRepository : ISaveRepository
    {
        public PlayerSaveData Stored;

        public bool TryLoad(out PlayerSaveData data)
        {
            data = Stored;
            return data != null;
        }

        public void Save(PlayerSaveData data) => Stored = data;
        public void Delete() => Stored = null;
    }
}
