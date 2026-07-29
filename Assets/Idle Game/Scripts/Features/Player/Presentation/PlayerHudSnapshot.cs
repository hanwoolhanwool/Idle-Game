/// <summary>
/// HUD 렌더링에 필요한 값만 담은 읽기 전용 스냅샷(DTO).
/// 프레젠테이션이 StatMachine 내부 구조에 직접 의존하지 않도록 경계를 만든다.
/// <para>
/// 전투 수치(스탯)와 성장·재화가 한 구조체에 모여 있는 이유는, HUD가 "한 시점의 플레이어 전체"를
/// 한 번에 그리기 때문이다. 출처가 셋(StatMachine·Progression·Wallet)이어도 표시 시점은 하나다.
/// </para>
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

    public readonly int Level;
    public readonly int Exp;

    /// <summary>다음 레벨까지 필요한 경험치. 최고 레벨이면 <see cref="int.MaxValue"/>.</summary>
    public readonly int RequiredExp;

    public readonly long Gold;

    /// <summary>최고 레벨이라 더 이상 경험치 진행도가 의미 없는 상태.</summary>
    public bool IsMaxLevel => RequiredExp == int.MaxValue;

    public PlayerHudSnapshot(
        float currentHp, float maxHp,
        float currentMp, float maxMp,
        float attackPower, float attackSpeed, float moveSpeed, float dps,
        int level, int exp, int requiredExp, long gold)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;
        CurrentMp = currentMp;
        MaxMp = maxMp;
        AttackPower = attackPower;
        AttackSpeed = attackSpeed;
        MoveSpeed = moveSpeed;
        Dps = dps;
        Level = level;
        Exp = exp;
        RequiredExp = requiredExp;
        Gold = gold;
    }
}
