/// <summary>
/// 적 처치 보상을 한 번에 실어 나르는 값 페이로드. 보상 종류가 늘어도(아이템 등)
/// 필드만 추가하면 되어 발행 시그니처가 바뀌지 않는다(OCP). 각 구독자는 자기가
/// 관심 있는 필드만 읽는다(ISP). 값 타입이라 불필요한 힙 할당이 없다.
/// </summary>
public readonly struct KillRewardPayload
{
    public readonly int Exp;
    public readonly int Gold;

    public KillRewardPayload(int exp, int gold)
    {
        Exp = exp;
        Gold = gold;
    }
}
