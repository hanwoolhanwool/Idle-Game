/// <summary>
/// 스탯의 최종 계산값을 읽기 전용으로 노출하는 추상.
/// 이동/전투/HUD 등 소비처는 <see cref="StatMachine"/> 구현 전체가 아니라
/// 이 인터페이스에만 의존해 최종 스탯을 조회한다. (ISP / DIP)
/// </summary>
public interface IReadOnlyStats
{
    /// <summary>지정한 스탯의 base + modifier가 모두 반영된 최종 값.</summary>
    float GetFinal(StatType statType);
}
