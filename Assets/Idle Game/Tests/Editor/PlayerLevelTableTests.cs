using NUnit.Framework;

/// <summary>
/// 레벨 성장 규칙의 단일 진실 공급원인 <see cref="PlayerLevelTable"/> 검증.
/// (M0 계획서 §10 케이스 1·2)
/// </summary>
public sealed class PlayerLevelTableTests
{
    private PlayerLevelTable _table;

    [TearDown]
    public void TearDown() => TestFactory.Destroy(_table);

    // ───────────────────────── 케이스 1: RequiredExp ─────────────────────────

    [Test]
    public void RequiredExp_레벨1은_BaseRequiredExp와_같다()
    {
        _table = TestFactory.CreateLevelTable(baseRequiredExp: 100, expGrowthRate: 1f);

        Assert.AreEqual(100, _table.RequiredExp(1));
    }

    [Test]
    public void RequiredExp_배율이_1보다_크면_레벨이_오를수록_단조증가한다()
    {
        _table = TestFactory.CreateLevelTable(maxLevel: 20, baseRequiredExp: 100, expGrowthRate: 1.5f);

        int previous = _table.RequiredExp(1);
        for (int level = 2; level < _table.MaxLevel; level++)
        {
            int current = _table.RequiredExp(level);
            Assert.Greater(current, previous, $"레벨 {level}의 필요 경험치가 이전 레벨보다 작습니다.");
            previous = current;
        }
    }

    [Test]
    public void RequiredExp_최고레벨_이상이면_int_MaxValue를_반환한다()
    {
        _table = TestFactory.CreateLevelTable(maxLevel: 10);

        // 최고 레벨에서는 "다음 레벨"이 존재하지 않는다. 0이나 음수로 표현하면
        // 호출부가 "경험치 0으로도 레벨업 가능"으로 오해할 수 있어 도달 불가능한 값으로 막는다.
        Assert.AreEqual(int.MaxValue, _table.RequiredExp(10));
        Assert.AreEqual(int.MaxValue, _table.RequiredExp(999));
    }

    [Test]
    public void RequiredExp_레벨이_1미만이면_레벨1로_취급한다()
    {
        _table = TestFactory.CreateLevelTable(baseRequiredExp: 100, expGrowthRate: 1f);

        // 손상된 세이브가 레벨 0이나 음수를 넘겨도 예외 대신 안전한 값으로 흐른다.
        Assert.AreEqual(_table.RequiredExp(1), _table.RequiredExp(0));
        Assert.AreEqual(_table.RequiredExp(1), _table.RequiredExp(-5));
    }

    // ───────────────────────── 케이스 2: ResolveStats ─────────────────────────

    [Test]
    public void ResolveStats_레벨1은_BaseValue를_그대로_돌려준다()
    {
        _table = TestFactory.CreateLevelTable();

        PlayerBaseStatSet stats = _table.ResolveStats(1);

        Assert.AreEqual(10f, stats.GetOrDefault(StatType.AttackPower), 0.001f);
        Assert.AreEqual(100f, stats.GetOrDefault(StatType.MaxHp), 0.001f);
    }

    [Test]
    public void ResolveStats_레벨N은_Base더하기_PerLevel곱하기_N빼기1이다()
    {
        _table = TestFactory.CreateLevelTable(maxLevel: 10);

        PlayerBaseStatSet stats = _table.ResolveStats(5);

        // AttackPower = 10 + 5 × (5-1) = 30
        Assert.AreEqual(30f, stats.GetOrDefault(StatType.AttackPower), 0.001f);
        // MaxHp = 100 + 20 × 4 = 180
        Assert.AreEqual(180f, stats.GetOrDefault(StatType.MaxHp), 0.001f);
    }

    [Test]
    public void ResolveStats_PerLevel이_0인_스탯은_레벨과_무관하게_고정된다()
    {
        _table = TestFactory.CreateLevelTable(maxLevel: 10);

        float atLevel1 = _table.ResolveStats(1).GetOrDefault(StatType.MoveSpeed);
        float atLevel10 = _table.ResolveStats(10).GetOrDefault(StatType.MoveSpeed);

        // 이동속도가 레벨에 따라 변하면 조작감이 성장에 휘둘린다(의도된 고정).
        Assert.AreEqual(atLevel1, atLevel10, 0.001f);
    }

    [Test]
    public void ResolveStats_최고레벨을_넘는_입력은_최고레벨로_클램프된다()
    {
        _table = TestFactory.CreateLevelTable(maxLevel: 10);

        float atMax = _table.ResolveStats(10).GetOrDefault(StatType.AttackPower);
        float beyondMax = _table.ResolveStats(50).GetOrDefault(StatType.AttackPower);

        // 밸런스 패치로 상한이 내려간 뒤 예전 세이브가 로드되는 상황(정본 §6.8).
        Assert.AreEqual(atMax, beyondMax, 0.001f);
    }

    [Test]
    public void ResolveStats_Growths가_비어도_예외없이_빈_집합을_돌려준다()
    {
        _table = TestFactory.CreateLevelTable();
        _table.Growths = null;

        PlayerBaseStatSet stats = _table.ResolveStats(5);

        Assert.IsNotNull(stats);
        Assert.IsEmpty(stats.Entries);
    }
}
