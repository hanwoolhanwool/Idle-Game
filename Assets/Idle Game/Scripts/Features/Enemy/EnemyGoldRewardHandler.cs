using System;

/// <summary>
/// 적 처치 보상 허브(<see cref="EnemyKillReward"/>)와 골드 수신 측(<see cref="IGoldReceiver"/>)을
/// 잇는 브리지 어댑터. <c>EnemyExpRewardHandler</c>와 완전 대칭 구조다.
/// 구독형이므로 <see cref="IDisposable"/>로 수명을 관리한다.
/// </summary>
public sealed class EnemyGoldRewardHandler : IDisposable
{
    private readonly IGoldReceiver _receiver;

    public EnemyGoldRewardHandler(IGoldReceiver receiver)
    {
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        EnemyKillReward.Rewarded += HandleRewarded;
    }

    private void HandleRewarded(KillRewardPayload payload)
    {
        _receiver.AddGold(payload.Gold);
    }

    public void Dispose()
    {
        EnemyKillReward.Rewarded -= HandleRewarded;
    }
}
