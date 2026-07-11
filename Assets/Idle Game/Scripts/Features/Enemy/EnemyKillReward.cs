using System;
using UnityEngine;

/// <summary>
/// 적 처치 보상을 발행하는 정적 이벤트 허브.
/// 적(<see cref="EnemyUnit"/>)은 여기에 보상을 <b>발행만</b> 하고 누가 받는지 모른다.
/// 브리지(<see cref="EnemyExpRewardHandler"/>)가 <see cref="Rewarded"/>를 구독해 성장 도메인으로 전달한다.
/// 적↔성장의 직접 참조를 끊는 유일한 결합 완충 지점이다(DIP, combat.md의 IDamageable과 같은 결).
/// </summary>
public static class EnemyKillReward
{
    /// <summary>처치 보상이 발행될 때마다 획득 경험치를 전달한다. 허브는 상태를 갖지 않아 발행마다 독립적으로 누적된다.</summary>
    public static event Action<int> Rewarded;

    /// <summary>처치 보상을 발행한다. <paramref name="exp"/>가 0 이하이면 무시한다(오지급 방어).</summary>
    public static void Publish(int exp)
    {
        if (exp <= 0)
            return;

        Rewarded?.Invoke(exp);
    }

    /// <summary>
    /// 도메인 리로드 비활성("Enter Play Mode Options") 시 이전 세션 구독자가 잔류하는 것을 막는다.
    /// 새 플레이 세션 진입마다 구독자를 초기화한다(combat.md의 정적 상태 잔류 주의와 동일).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Rewarded = null;
    }
}
