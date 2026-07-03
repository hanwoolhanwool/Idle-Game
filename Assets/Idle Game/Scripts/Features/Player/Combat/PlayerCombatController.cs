public sealed class PlayerCombatController
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

    public void TakeDamage(float damage)
    {
        _statComponent.ApplyDamage(damage);
    }
}