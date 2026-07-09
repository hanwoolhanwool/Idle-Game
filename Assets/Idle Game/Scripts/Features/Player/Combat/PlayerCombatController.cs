/// <summary>
/// 플레이어의 피격 수신 창구. 적/플레이어를 <see cref="IDamageable"/>로 일원화해
/// 피격 소스가 대상 종류를 몰라도 동일하게 데미지를 가할 수 있게 한다.
/// </summary>
public sealed class PlayerCombatController : IDamageable
{
    private readonly PlayerStatComponent _statComponent;

    public PlayerCombatController(PlayerStatComponent statComponent)
    {
        _statComponent = statComponent;
    }

    public float GetExpectedDamagePerHit()
    {
        return _statComponent.ComputeFinalDamagePerHit();
    }

    public float GetExpectedDps()
    {
        return _statComponent.ComputeDps();
    }

    public void ApplyDamage(float damage)
    {
        _statComponent.ApplyDamage(damage);
    }
}