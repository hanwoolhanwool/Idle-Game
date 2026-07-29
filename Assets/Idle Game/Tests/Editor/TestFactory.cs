using UnityEngine;

/// <summary>
/// 테스트가 공통으로 쓰는 객체 조립 헬퍼.
/// <para>
/// 생성자 의존성을 테스트마다 되풀이해 적으면, 프로덕션 생성자가 한 번 바뀔 때 테스트 전부를
/// 손봐야 한다. 조립을 여기 한 곳에 모아 <b>변경 지점을 하나로</b> 만든다.
/// </para>
/// </summary>
internal static class TestFactory
{
    /// <summary>
    /// 검증하기 쉬운 값으로 고정한 레벨 테이블.
    /// 프로덕션 기본값(1.12배 등)을 쓰지 않는 이유는, 밸런스 조정으로 기본값이 바뀌면
    /// 테스트가 함께 깨지기 때문이다. 테스트는 <b>규칙</b>을 검증하지 밸런스 수치를 검증하지 않는다.
    /// </summary>
    public static PlayerLevelTable CreateLevelTable(
        int maxLevel = 10,
        int baseRequiredExp = 100,
        float expGrowthRate = 1f)
    {
        var table = ScriptableObject.CreateInstance<PlayerLevelTable>();
        table.MaxLevel = maxLevel;
        table.BaseRequiredExp = baseRequiredExp;
        table.ExpGrowthRate = expGrowthRate;
        table.Growths = new[]
        {
            new StatGrowthEntry { Type = StatType.MaxHp,       BaseValue = 100f, PerLevel = 20f },
            new StatGrowthEntry { Type = StatType.AttackPower, BaseValue = 10f,  PerLevel = 5f },
            // 성장하지 않는 스탯도 하나 둔다 — PerLevel=0이 레벨과 무관하게 유지되는지 확인하기 위함.
            new StatGrowthEntry { Type = StatType.MoveSpeed,   BaseValue = 7f,   PerLevel = 0f },
        };
        return table;
    }

    public static PlayerProgressionConfig CreateConfig(int startLevel = 1, int startExp = 0, int promotionTier = 0)
    {
        var config = ScriptableObject.CreateInstance<PlayerProgressionConfig>();
        config.StartLevel = startLevel;
        config.StartExp = startExp;
        config.PromotionTier = promotionTier;
        return config;
    }

    /// <summary>
    /// 성장 컨트롤러를 실제 의존 그래프(리졸버 → 테이블, 오케스트레이터 → 스탯 머신)로 조립한다.
    /// 목(mock)을 쓰지 않는 이유는 이 체인 자체가 결함 1의 회귀 지점이기 때문이다 —
    /// 가짜 리졸버를 끼우면 "레벨이 스탯에 반영되는가"를 검증할 수 없다.
    /// </summary>
    public static PlayerProgressionController CreateController(
        PlayerLevelTable table,
        int startLevel = 1,
        int startExp = 0,
        PlayerStatComponent statComponent = null)
    {
        statComponent ??= new PlayerStatComponent();
        return new PlayerProgressionController(
            CreateConfig(startLevel, startExp),
            table,
            new PlayerBaseStatResolver(table),
            new PlayerStatOrchestrator(statComponent));
    }

    /// <summary>ScriptableObject는 GC 대상이 아니라 명시적으로 파괴해야 에디터 메모리에 누적되지 않는다.</summary>
    public static void Destroy(Object obj)
    {
        if (obj != null)
            Object.DestroyImmediate(obj);
    }
}
