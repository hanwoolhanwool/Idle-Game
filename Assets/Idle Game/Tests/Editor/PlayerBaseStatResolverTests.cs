using System;
using NUnit.Framework;

/// <summary>
/// <see cref="PlayerBaseStatResolver"/>가 레벨을 <b>실제로</b> 스탯에 반영하는지 검증한다.
/// <para>
/// 이것은 회귀 테스트다. 과거 구현은 <c>PlayerProgressionState</c>를 인자로 받고도 무시한 채
/// config의 시작 스탯을 그대로 돌려줬고, 그래서 레벨 1과 레벨 100의 스탯이 같았다.
/// 성장 루프 전체가 무의미해지는 결함이었으므로 반드시 잠가 둔다.
/// (M0 계획서 §10 케이스 3)
/// </para>
/// </summary>
public sealed class PlayerBaseStatResolverTests
{
    private PlayerLevelTable _table;

    [SetUp]
    public void SetUp() => _table = TestFactory.CreateLevelTable(maxLevel: 100);

    [TearDown]
    public void TearDown() => TestFactory.Destroy(_table);

    [Test]
    public void Resolve_레벨1과_레벨50의_결과가_다르다()
    {
        var resolver = new PlayerBaseStatResolver(_table);

        float atLevel1 = resolver
            .Resolve(new PlayerProgressionState { Level = 1 })
            .GetOrDefault(StatType.AttackPower);

        float atLevel50 = resolver
            .Resolve(new PlayerProgressionState { Level = 50 })
            .GetOrDefault(StatType.AttackPower);

        Assert.AreNotEqual(atLevel1, atLevel50,
            "레벨이 베이스 스탯에 반영되지 않습니다 — 성장 루프의 마지막 링크가 끊겼습니다.");
        Assert.Greater(atLevel50, atLevel1);
    }

    [Test]
    public void Resolve_테이블의_계산결과와_정확히_일치한다()
    {
        var resolver = new PlayerBaseStatResolver(_table);

        float resolved = resolver
            .Resolve(new PlayerProgressionState { Level = 7 })
            .GetOrDefault(StatType.AttackPower);

        // 리졸버는 스스로 공식을 갖지 않고 테이블에 위임하기만 한다(SRP).
        float expected = _table.ResolveStats(7).GetOrDefault(StatType.AttackPower);
        Assert.AreEqual(expected, resolved, 0.001f);
    }

    [Test]
    public void Resolve_상태가_null이면_레벨1로_취급한다()
    {
        var resolver = new PlayerBaseStatResolver(_table);

        float resolved = resolver.Resolve(null).GetOrDefault(StatType.AttackPower);
        float atLevel1 = _table.ResolveStats(1).GetOrDefault(StatType.AttackPower);

        // 조립 실패로 상태가 비어도 앱이 죽는 대신 최소 스탯으로 흐른다.
        Assert.AreEqual(atLevel1, resolved, 0.001f);
    }

    [Test]
    public void 생성자_테이블이_없으면_즉시_예외를_던진다()
    {
        // 테이블 없는 리졸버는 어떤 스탯도 산출할 수 없다. 조용히 0을 돌려주면
        // "왜 캐릭터가 약한가"를 런타임 내내 추적하게 되므로 조립 시점에 터뜨린다.
        Assert.Throws<ArgumentNullException>(() => new PlayerBaseStatResolver(null));
    }
}
