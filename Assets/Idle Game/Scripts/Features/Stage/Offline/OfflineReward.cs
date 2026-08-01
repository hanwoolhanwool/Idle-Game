/// <summary>
/// 오프라인 정산 결과(값 타입). 지급 주체와 계산 주체를 분리하기 위한 운반 그릇이다.
/// </summary>
public readonly struct OfflineReward
{
    /// <summary>정산에 반영된 추정 처치 수(표시·디버그용).</summary>
    public readonly int Kills;

    public readonly int Exp;
    public readonly long Gold;

    /// <summary>실제로 지급할 것이 있는가. 0 보상일 때 UI·로그를 띄우지 않기 위한 판별.</summary>
    public bool HasReward => Exp > 0 || Gold > 0;

    public static OfflineReward None => new(0, 0, 0L);

    public OfflineReward(int kills, int exp, long gold)
    {
        Kills = kills;
        Exp = exp;
        Gold = gold;
    }
}
