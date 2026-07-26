using System;

/// <summary>
/// 적 처치 보상 허브(<see cref="EnemyKillReward"/>)와 경험치 수신 측(<see cref="IExpReceiver"/>)을
/// 잇는 유일한 브리지 어댑터. 두 독립 도메인이 서로를 모른 채 결합되도록 한 지점에 격리한다.
/// 구독형이므로 <see cref="IDisposable"/>로 수명을 관리한다(combat.md의 PlayerDeathHandler와 동일 패턴).
/// </summary>
public sealed class EnemyExpRewardHandler : IDisposable
{
    private readonly IExpReceiver _receiver;

    public EnemyExpRewardHandler(IExpReceiver receiver)
    {
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        EnemyKillReward.Rewarded += HandleRewarded;
    }

    private void HandleRewarded(KillRewardPayload payload)
    {
        _receiver.AddExp(payload.Exp);
    }

    public void Dispose()
    {
        EnemyKillReward.Rewarded -= HandleRewarded;
    }
}
