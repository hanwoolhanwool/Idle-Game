using System;

/// <summary>
/// 자리를 비운 시간을 보상으로 환산한다.
/// <para>
/// <b>정적 순수 함수</b>다. 입력(경과 시간·처치율·스테이지·설정)만으로 출력이 정해지고
/// 상태도 Unity 의존도 없다. 특히 시간을 <b>인자로 받는 것</b>이 핵심이다 — 내부에서
/// <c>DateTime.UtcNow</c>를 읽으면 "8시간 비웠을 때"를 테스트할 방법이 사라진다.
/// </para>
/// </summary>
public static class OfflineRewardCalculator
{
    public static OfflineReward Calculate(
        TimeSpan elapsed,
        float killsPerSecond,
        StageDefinition stage,
        OfflineRewardConfig config)
    {
        // 기준선이 없거나(첫 실행) 시계를 과거로 되돌린 경우. 예외 대신 0 보상으로 흘린다.
        if (elapsed <= TimeSpan.Zero)
            return OfflineReward.None;

        // 한 번도 싸우지 않은 세션이 저장됐다면 환산할 성과가 없다.
        if (killsPerSecond <= 0f || stage == null)
            return OfflineReward.None;

        float maxHours = config != null ? config.MaxOfflineHours : 0f;
        float efficiency = config != null ? config.OfflineEfficiency : 0f;

        if (maxHours <= 0f || efficiency <= 0f)
            return OfflineReward.None;

        // 상한 클램프. 시계를 미래로 돌려도 이 선을 넘는 이득은 없다
        // (완전 방어는 서버 시각이 필요하다 — server-application-plan.md 범위).
        double seconds = Math.Min(elapsed.TotalSeconds, maxHours * 3600d);

        double estimatedKills = killsPerSecond * seconds * efficiency;
        if (estimatedKills < 1d)
            return OfflineReward.None;

        // 방치형은 자릿수가 빠르게 커진다. int로 곱하면 중후반에 조용히 넘치므로
        // 곱셈은 long/double로 하고 마지막에만 각 수신 계약의 타입으로 좁힌다.
        long kills = (long)estimatedKills;
        long exp = kills * stage.ExpReward;
        long gold = kills * stage.GoldReward;

        return new OfflineReward(
            ClampToInt(kills),
            ClampToInt(exp),
            gold);
    }

    /// <summary><see cref="IExpReceiver"/>가 int를 받으므로 상한에서 잘라 낸다(음수 방지 포함).</summary>
    private static int ClampToInt(long value)
    {
        if (value <= 0L)
            return 0;

        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }
}
