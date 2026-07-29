using NUnit.Framework;

/// <summary>
/// 경험치 누적·레벨업 절차 검증. (M0 계획서 §10 케이스 4·5)
/// </summary>
public sealed class PlayerProgressionControllerTests
{
    private PlayerLevelTable _table;

    [SetUp]
    public void SetUp()
    {
        // 배율 1 = 모든 레벨의 필요 경험치가 100. 이월·다중 레벨업을 암산으로 검증할 수 있다.
        _table = TestFactory.CreateLevelTable(maxLevel: 10, baseRequiredExp: 100, expGrowthRate: 1f);
    }

    [TearDown]
    public void TearDown() => TestFactory.Destroy(_table);

    // ───────────────────────── 케이스 4: 경험치 이월 ─────────────────────────

    [Test]
    public void AddExp_필요량을_넘긴_초과분은_다음_레벨로_이월된다()
    {
        var controller = TestFactory.CreateController(_table);

        controller.AddExp(150);

        Assert.AreEqual(2, controller.State.Level);
        // 150 - 100(레벨업 비용) = 50이 남는다. 버려지면 성장이 느려지고 유저가 손해를 본다.
        Assert.AreEqual(50, controller.State.Exp);
    }

    [Test]
    public void AddExp_한번의_지급으로_여러_레벨이_오른다()
    {
        var controller = TestFactory.CreateController(_table);

        // 오프라인 보상·보스 처치는 한 번에 수백 레벨분을 줄 수 있다.
        controller.AddExp(350);

        Assert.AreEqual(4, controller.State.Level);
        Assert.AreEqual(50, controller.State.Exp);
    }

    [Test]
    public void AddExp_레벨업이_베이스_스탯에_즉시_반영된다()
    {
        var statComponent = new PlayerStatComponent();
        var controller = TestFactory.CreateController(_table, statComponent: statComponent);

        float before = statComponent.Stats.GetFinal(StatType.AttackPower);
        controller.AddExp(100);
        float after = statComponent.Stats.GetFinal(StatType.AttackPower);

        // 레벨만 오르고 스탯이 그대로면 성장 루프가 닫히지 않는다.
        Assert.Greater(after, before);
    }

    [Test]
    public void AddExp_0이하는_무시한다()
    {
        var controller = TestFactory.CreateController(_table);

        controller.AddExp(0);
        controller.AddExp(-500);

        Assert.AreEqual(1, controller.State.Level);
        Assert.AreEqual(0, controller.State.Exp);
    }

    [Test]
    public void AddExp_변경을_이벤트로_알린다()
    {
        var controller = TestFactory.CreateController(_table);
        int calls = 0;
        controller.ProgressChanged += () => calls++;

        controller.AddExp(30);

        // 레벨이 오르지 않은 경험치 획득은 어떤 스탯도 바꾸지 않는다.
        // 이 이벤트가 없으면 HUD의 경험치 바가 레벨업 순간까지 멈춰 보인다.
        Assert.AreEqual(1, calls);
    }

    // ───────────────────────── 케이스 5: 최고 레벨 ─────────────────────────

    [Test]
    public void AddExp_최고레벨에_도달하면_더_오르지_않는다()
    {
        var controller = TestFactory.CreateController(_table);

        // 필요량의 수십 배를 한 번에 넣어도 상한을 넘지 못한다.
        controller.AddExp(999_999);

        Assert.AreEqual(_table.MaxLevel, controller.State.Level);
        Assert.IsTrue(controller.IsMaxLevel);
    }

    [Test]
    public void AddExp_최고레벨에서는_잔여_경험치를_보관하지_않는다()
    {
        var controller = TestFactory.CreateController(_table);

        controller.AddExp(999_999);

        // 상한에서 경험치가 쌓이면 표시(3.7M/∞)도 저장도 의미가 없다. 0으로 수렴시킨다.
        Assert.AreEqual(0, controller.State.Exp);
    }

    [Test]
    public void AddExp_최고레벨_도달_후의_추가_지급은_무시된다()
    {
        var controller = TestFactory.CreateController(_table, startLevel: 10);

        controller.AddExp(5000);

        Assert.AreEqual(10, controller.State.Level);
        Assert.AreEqual(0, controller.State.Exp);
    }

    [Test]
    public void 생성자_시작레벨이_상한을_넘으면_클램프된다()
    {
        // 밸런스 패치로 MaxLevel이 내려간 뒤 예전 config가 남아 있는 경우.
        var controller = TestFactory.CreateController(_table, startLevel: 999);

        Assert.AreEqual(_table.MaxLevel, controller.State.Level);
    }

    [Test]
    public void RequiredExpForNextLevel_최고레벨이면_int_MaxValue다()
    {
        var controller = TestFactory.CreateController(_table, startLevel: 10);

        Assert.AreEqual(int.MaxValue, controller.RequiredExpForNextLevel);
    }
}
