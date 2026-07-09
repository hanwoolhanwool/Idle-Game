using System;

/// <summary>
/// 피격(생존) 시 상태머신을 Hit(경직)로 전이시키는 어댑터(§3-F의 NotifyHit 지점).
/// Idle/Move에서만 반응해 시전(Casting)·사망(Dead)을 중단하지 않는다.
/// 이미 Hit 중이면 상태머신이 재전이를 무시하므로 경직이 갱신되지 않는다.
/// </summary>
public sealed class PlayerHitReaction : IDisposable
{
    private readonly PlayerStatComponent _statComponent;
    private readonly PlayerStateMachine _stateMachine;

    public PlayerHitReaction(PlayerStatComponent statComponent, PlayerStateMachine stateMachine)
    {
        _statComponent = statComponent ?? throw new ArgumentNullException(nameof(statComponent));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));

        _statComponent.OnDamaged += HandleDamaged;
    }

    private void HandleDamaged(float appliedDamage)
    {
        if (_statComponent.IsDead)
            return;

        var current = _stateMachine.CurrentStateID;
        if (current == PlayerStateID.Idle || current == PlayerStateID.Move)
            _stateMachine.TryChangeState(PlayerStateID.Hit);
    }

    public void Dispose()
    {
        if (_statComponent != null)
            _statComponent.OnDamaged -= HandleDamaged;
    }
}
