using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 세이브 왕복·손상 복구·저장 정책 검증. (M0 계획서 §10 케이스 8·9)
/// </summary>
public sealed class SaveSystemTests
{
    private string _directory;

    [SetUp]
    public void SetUp()
    {
        // 테스트마다 격리된 폴더를 쓴다. 공유하면 앞 테스트의 잔여 파일이 뒤 테스트를 오염시킨다.
        _directory = Path.Combine(Path.GetTempPath(), "IdleGameSaveTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // 정리 실패가 테스트 결과를 뒤집지는 않는다(임시 폴더는 OS가 회수한다).
        }
    }

    private string SavePath => Path.Combine(_directory, "save.json");

    // ───────────────────────── 케이스 8: 세이브 왕복 ─────────────────────────

    [Test]
    public void 왕복_레벨과_경험치와_골드가_그대로_돌아온다()
    {
        var repository = new FileSaveRepository(_directory);

        var saved = new PlayerSaveData();
        saved.Progression.Level = 27;
        saved.Progression.Exp = 1450;
        saved.Progression.PromotionTier = 2;
        saved.Wallet.SetAmount(PlayerWallet.GoldCurrencyId, 9876543210L);

        repository.Save(saved);

        Assert.IsTrue(repository.TryLoad(out PlayerSaveData loaded));
        Assert.AreEqual(27, loaded.Progression.Level);
        Assert.AreEqual(1450, loaded.Progression.Exp);
        Assert.AreEqual(2, loaded.Progression.PromotionTier);
        // long을 그대로 유지하는지가 중요하다. float/int로 좁혀지면 후반부 재화가 조용히 잘린다.
        Assert.AreEqual(9876543210L, loaded.Wallet.GetAmount(PlayerWallet.GoldCurrencyId));
    }

    [Test]
    public void 왕복_도메인_객체를_거쳐도_상태가_보존된다()
    {
        PlayerLevelTable table = TestFactory.CreateLevelTable(maxLevel: 100, expGrowthRate: 1f);
        try
        {
            var repository = new FileSaveRepository(_directory);

            // 저장 측: 실제 도메인 객체가 자기 섹션을 채운다.
            PlayerProgressionController source = TestFactory.CreateController(table);
            source.AddExp(350);
            var sourceWallet = new PlayerWallet();
            sourceWallet.AddGold(4200);

            var data = new PlayerSaveData();
            source.CaptureState(data);
            sourceWallet.CaptureState(data);
            repository.Save(data);

            // 복원 측: 신규 상태의 객체에 읽어 넣는다.
            Assert.IsTrue(repository.TryLoad(out PlayerSaveData loaded));
            PlayerProgressionController restored = TestFactory.CreateController(table);
            var restoredWallet = new PlayerWallet();
            restored.RestoreState(loaded);
            restoredWallet.RestoreState(loaded);

            Assert.AreEqual(source.State.Level, restored.State.Level);
            Assert.AreEqual(source.State.Exp, restored.State.Exp);
            Assert.AreEqual(sourceWallet.Gold, restoredWallet.Gold);
        }
        finally
        {
            TestFactory.Destroy(table);
        }
    }

    [Test]
    public void 복원은_저장된_스탯이_아니라_레벨로_재계산한다()
    {
        PlayerLevelTable table = TestFactory.CreateLevelTable(maxLevel: 100, expGrowthRate: 1f);
        try
        {
            var data = new PlayerSaveData();
            data.Progression.Level = 20;

            var statComponent = new PlayerStatComponent();
            PlayerProgressionController controller =
                TestFactory.CreateController(table, statComponent: statComponent);
            controller.RestoreState(data);

            // "원인(레벨)을 저장하고 결과(스탯)는 재계산한다" — 밸런스 패치가 로드 즉시 반영되는 근거.
            float expected = table.ResolveStats(20).GetOrDefault(StatType.AttackPower);
            Assert.AreEqual(expected, statComponent.Stats.GetFinal(StatType.AttackPower), 0.001f);
        }
        finally
        {
            TestFactory.Destroy(table);
        }
    }

    // ───────────────────────── 케이스 9: 손상 복구 ─────────────────────────

    [Test]
    public void 손상된_세이브는_예외없이_실패를_반환한다()
    {
        var repository = new FileSaveRepository(_directory);
        File.WriteAllText(SavePath, "{ this is not json ###");

        bool loaded = false;
        // 앱이 죽는 대신 "저장본 없음"으로 흘러 신규 게임이 시작되어야 한다.
        Assert.DoesNotThrow(() => loaded = repository.TryLoad(out _));
        Assert.IsFalse(loaded);
    }

    [Test]
    public void 손상된_세이브는_백업에서_복구된다()
    {
        var repository = new FileSaveRepository(_directory);

        // 1차 저장 → save.json 생성(백업 없음)
        var first = new PlayerSaveData();
        first.Progression.Level = 5;
        repository.Save(first);

        // 2차 저장 → 1차 내용이 save.bak으로 백업된다
        var second = new PlayerSaveData();
        second.Progression.Level = 9;
        repository.Save(second);

        // 본 파일만 손상시킨다(쓰기 도중 전원이 끊긴 상황의 재현)
        File.WriteAllText(SavePath, "corrupted");

        Assert.IsTrue(repository.TryLoad(out PlayerSaveData recovered));
        // 최신분은 잃더라도 진행 전체를 잃지는 않는다 — 원자적 쓰기 + 백업의 목적.
        Assert.AreEqual(5, recovered.Progression.Level);
    }

    [Test]
    public void 세이브가_없으면_실패를_반환한다()
    {
        var repository = new FileSaveRepository(_directory);

        // 첫 실행은 정상 경로다. 예외로 다루면 신규 유저마다 에러 로그가 쌓인다.
        Assert.IsFalse(repository.TryLoad(out _));
    }

    [Test]
    public void Delete는_본파일과_백업을_모두_지운다()
    {
        var repository = new FileSaveRepository(_directory);
        repository.Save(new PlayerSaveData());
        repository.Save(new PlayerSaveData());

        repository.Delete();

        Assert.IsFalse(repository.TryLoad(out _));
        Assert.IsFalse(File.Exists(SavePath));
    }

    // ───────────────────────── SaveService 정책 ─────────────────────────

    [Test]
    public void LoadAndRestore_저장본이_없으면_아무것도_복원하지_않는다()
    {
        var repository = new FakeSaveRepository();
        var service = new SaveService(repository);
        var saveable = new SpySaveable();
        service.Register(saveable);

        service.LoadAndRestore();

        // 신규 게임에서는 각 도메인이 이미 자기 기본값을 갖고 있다. 덮어쓰면 config가 무시된다.
        Assert.AreEqual(0, saveable.RestoreCount);
    }

    [Test]
    public void SaveNow_등록된_모든_조각을_수집해_한번에_저장한다()
    {
        var repository = new FakeSaveRepository();
        var service = new SaveService(repository);
        var a = new SpySaveable();
        var b = new SpySaveable();
        service.Register(a);
        service.Register(b);

        service.SaveNow();

        Assert.AreEqual(1, a.CaptureCount);
        Assert.AreEqual(1, b.CaptureCount);
        Assert.AreEqual(1, repository.SaveCount);
    }

    [Test]
    public void Register_같은_대상을_두_번_등록해도_한_번만_수집한다()
    {
        var repository = new FakeSaveRepository();
        var service = new SaveService(repository);
        var saveable = new SpySaveable();

        service.Register(saveable);
        service.Register(saveable);
        service.SaveNow();

        Assert.AreEqual(1, saveable.CaptureCount);
    }

    [Test]
    public void Tick_간격에_도달하기_전에는_저장하지_않는다()
    {
        var repository = new FakeSaveRepository();
        var service = new SaveService(repository, autoSaveInterval: 10f);

        service.Tick(9.9f);

        Assert.AreEqual(0, repository.SaveCount);
    }

    [Test]
    public void Tick_저장_직후_타이머가_리셋되어_매프레임_저장하지_않는다()
    {
        var repository = new FakeSaveRepository();
        var service = new SaveService(repository, autoSaveInterval: 10f);

        service.Tick(10f);    // 여기서 1회 저장
        service.Tick(0.016f); // 다음 프레임
        service.Tick(0.016f);

        // 리셋을 빠뜨리면(= 대신 += 0f) 이후 매 프레임 JSON 직렬화 + 파일 쓰기가 일어나
        // 모바일에서 프레임이 무너진다. 이 테스트가 그 회귀를 막는다.
        Assert.AreEqual(1, repository.SaveCount);
    }

    [Test]
    public void SaveNow_수동_저장도_주기_타이머를_되감는다()
    {
        var repository = new FakeSaveRepository();
        var service = new SaveService(repository, autoSaveInterval: 10f);

        service.Tick(9f);
        service.SaveNow();  // 앱 일시정지 등 외부 요청
        service.Tick(2f);   // 누적이 리셋됐다면 아직 11초가 아니라 2초다

        Assert.AreEqual(1, repository.SaveCount);
    }

    [Test]
    public void 앱보다_높은_버전의_세이브는_복원도_저장도_거부한다()
    {
        var repository = new FakeSaveRepository
        {
            Stored = new PlayerSaveData { Version = SaveService.CurrentVersion + 1 }
        };
        var service = new SaveService(repository);
        var saveable = new SpySaveable();
        service.Register(saveable);

        LogAssert.Expect(LogType.Error, new Regex("세이브 버전"));
        service.LoadAndRestore();

        Assert.IsTrue(service.IsSaveBlocked);
        Assert.AreEqual(0, saveable.RestoreCount);

        // 구버전 앱이 신버전 세이브를 덮어쓰면 유저 데이터가 파괴된다. 저장 자체를 막는다.
        service.SaveNow();
        Assert.AreEqual(0, repository.SaveCount);
    }

    [Test]
    public void 마이그레이션이_없어도_같은_버전_세이브는_그대로_복원된다()
    {
        var repository = new FakeSaveRepository
        {
            Stored = new PlayerSaveData { Version = SaveService.CurrentVersion }
        };
        var service = new SaveService(repository);
        var saveable = new SpySaveable();
        service.Register(saveable);

        service.LoadAndRestore();

        Assert.AreEqual(1, saveable.RestoreCount);
        Assert.IsFalse(service.IsSaveBlocked);
    }

    [Test]
    public void 등록된_마이그레이션이_적용되고_버전이_올라간다()
    {
        var repository = new FakeSaveRepository
        {
            Stored = new PlayerSaveData { Version = 0 }
        };
        var step = new SpyMigration { FromVersion = 0 };
        var service = new SaveService(repository, 60f, new List<ISaveMigration> { step });
        var saveable = new SpySaveable();
        service.Register(saveable);

        service.LoadAndRestore();

        Assert.AreEqual(1, step.MigrateCount);
        Assert.AreEqual(1, saveable.RestoreCount);
    }

    [Test]
    public void 변환기가_없는_구버전_세이브도_무한루프에_빠지지_않는다()
    {
        var repository = new FakeSaveRepository
        {
            Stored = new PlayerSaveData { Version = 0 }
        };
        var service = new SaveService(repository); // 마이그레이션 등록 0개
        var saveable = new SpySaveable();
        service.Register(saveable);

        LogAssert.Expect(LogType.Warning, new Regex("마이그레이션이 없습니다"));

        // 변환기를 못 찾았을 때 버전을 올리지 않으면 while 루프가 영원히 돌아 앱이 멈춘다.
        Assert.DoesNotThrow(() => service.LoadAndRestore());
        Assert.AreEqual(1, saveable.RestoreCount);
    }

    // ───────────────────────── 테스트 대역(Test Double) ─────────────────────────

    /// <summary>
    /// 메모리 기반 저장소. 파일 I/O 없이 저장 <b>정책</b>만 검증하기 위한 대역이다.
    /// <see cref="ISaveRepository"/>가 추상이라 이런 교체가 가능하다(DIP의 실익).
    /// </summary>
    private sealed class FakeSaveRepository : ISaveRepository
    {
        public PlayerSaveData Stored;
        public int SaveCount;

        public bool TryLoad(out PlayerSaveData data)
        {
            data = Stored;
            return data != null;
        }

        public void Save(PlayerSaveData data)
        {
            Stored = data;
            SaveCount++;
        }

        public void Delete()
        {
            Stored = null;
        }
    }

    /// <summary>호출 횟수만 세는 저장 대상.</summary>
    private sealed class SpySaveable : ISaveable
    {
        public int CaptureCount;
        public int RestoreCount;

        public void CaptureState(PlayerSaveData data) => CaptureCount++;
        public void RestoreState(PlayerSaveData data) => RestoreCount++;
    }

    private sealed class SpyMigration : ISaveMigration
    {
        public int FromVersion { get; set; }
        public int MigrateCount;

        public void Migrate(PlayerSaveData data) => MigrateCount++;
    }
}
