using System;

/// <summary>
/// 레벨 테이블을 근거로 베이스 스탯을 산출하는 기본 구현.
///
/// <para>
/// 이전 구현은 <c>PlayerProgressionState</c>를 인자로 받고도 무시한 채 config의 시작 스탯을
/// 그대로 반환했다. 그 결과 레벨 1과 레벨 100의 스탯이 동일해, 경험치를 모아 레벨을 올려도
/// 캐릭터가 전혀 강해지지 않았다(성장 루프의 마지막 링크 단절).
/// 이제 레벨은 <see cref="PlayerLevelTable"/>을 통해 실제 스탯으로 환산된다.
/// </para>
/// </summary>
public sealed class PlayerBaseStatResolver : IPlayerBaseStatResolver
{
    private readonly PlayerLevelTable _table;

    public PlayerBaseStatResolver(PlayerLevelTable table)
    {
        _table = table != null
            ? table
            : throw new ArgumentNullException(nameof(table), "레벨 테이블 없이는 베이스 스탯을 산출할 수 없다.");
    }

    public PlayerBaseStatSet Resolve(PlayerProgressionState progressionState)
    {
        if (progressionState == null)
            return _table.ResolveStats(1);

        // 전직 차수(PromotionTier)는 아직 스탯에 반영하지 않는다.
        // 전직 시스템(M2)에서 차수별 보정을 이 지점에 합산한다 — 호출부는 그대로다(OCP).
        return _table.ResolveStats(progressionState.Level);
    }
}
