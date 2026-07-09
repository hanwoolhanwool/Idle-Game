/// <summary>
/// HUD 렌더링에 필요한 값만 담은 읽기 전용 스냅샷(DTO).
/// 프레젠테이션이 StatMachine 내부 구조에 직접 의존하지 않도록 경계를 만든다.
/// </summary>
public readonly struct PlayerHudSnapshot
{
    public readonly float CurrentHp;
    public readonly float MaxHp;
    public readonly float CurrentMp;
    public readonly float MaxMp;
    public readonly float AttackPower;
    public readonly float AttackSpeed;
    public readonly float MoveSpeed;
    public readonly float Dps;

    public PlayerHudSnapshot(
        float currentHp, float maxHp,
        float currentMp, float maxMp,
        float attackPower, float attackSpeed, float moveSpeed, float dps)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;
        CurrentMp = currentMp;
        MaxMp = maxMp;
        AttackPower = attackPower;
        AttackSpeed = attackSpeed;
        MoveSpeed = moveSpeed;
        Dps = dps;
    }
}
