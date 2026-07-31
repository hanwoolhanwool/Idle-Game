/// <summary>
/// 적 한 마리를 스폰할 때 주입하는 최종 수치 묶음(값 타입).
/// <para>
/// 인자를 나열하는 대신 페이로드로 감싸는 이유는 <c>KillRewardPayload</c>와 같다 —
/// 앞으로 주입할 값이 늘어도(공격력 배율·드랍 테이블·보스 패턴) <b>필드만 추가</b>하면 되고
/// 발행 시그니처와 호출부는 바뀌지 않는다(OCP). 값 타입이라 스폰 경로에 힙 할당도 없다.
/// </para>
/// <para>
/// 담기는 것은 <b>계산이 끝난 최종 수치</b>다. 적은 자기 체력이 얼마인지만 알면 되고,
/// 그 값이 어떤 스테이지의 어떤 배율에서 나왔는지는 몰라야 한다(SRP).
/// </para>
/// </summary>
public readonly struct EnemySpawnParams
{
    /// <summary>난이도 배율이 이미 반영된 최대 체력.</summary>
    public readonly float MaxHp;

    public readonly int ExpReward;
    public readonly int GoldReward;

    public EnemySpawnParams(float maxHp, int expReward, int goldReward)
    {
        // 체력 0 이하는 스폰 즉시 사망을 뜻해 스포너가 무한 스폰 루프에 빠진다.
        MaxHp = maxHp > 0f ? maxHp : 1f;
        ExpReward = expReward;
        GoldReward = goldReward;
    }
}
