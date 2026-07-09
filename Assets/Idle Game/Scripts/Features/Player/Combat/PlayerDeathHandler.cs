using System;

/// <summary>
/// 플레이어 사망 시 하위 시스템(스킬·상태머신·이동)을 연동하는 어댑터.
/// 자원 컴포넌트의 <see cref="PlayerStatComponent.OnDied"/>를 구독해
/// 시전 취소 → 이동 정지 → 상태머신 Dead 전이를 한 곳에서 조율한다. (SRP)
/// </summary>
public sealed class PlayerDeathHandler : IDisposable
{
    private readonly PlayerStatComponent _statComponent;
    private readonly PlayerSkillController _skillController;
    private readonly PlayerStateMachine _stateMachine;
    private readonly IPlayerMovementController _movementController;

    /// <summary>사망 처리가 완료된 뒤 발행(게임오버 UI 등 상위 연동용).</summary>
    public event Action OnPlayerDied;

    public PlayerDeathHandler(
        PlayerStatComponent statComponent,
        PlayerStateMachine stateMachine,
        PlayerSkillController skillController,
        IPlayerMovementController movementController)
    {
        _statComponent = statComponent ?? throw new ArgumentNullException(nameof(statComponent));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _skillController = skillController;
        _movementController = movementController;

        _statComponent.OnDied += HandleDied;
    }

    private void HandleDied()
    {
        _skillController?.CancelCast();
        _movementController?.SetMovementEnabled(false);

        _stateMachine.Context.SetDead(true);
        _stateMachine.TryChangeState(PlayerStateID.Dead);

        OnPlayerDied?.Invoke();
    }

    public void Dispose()
    {
        if (_statComponent != null)
            _statComponent.OnDied -= HandleDied;
    }
}
