using System;
using UnityEngine;

/// <summary>
/// 현재 플레이어를 피격 대상(<see cref="IDamageable"/>)과 위치로 노출하는 전역 접근점.
/// 적이 플레이어를 찾을 수 있게 한다(적을 찾는 EnemyRegistry와 대칭). 플레이어는 단일이므로 1개만 보관.
/// 장기적으로는 주입 가능한 서비스로 대체 가능(§3-H와 동일한 트레이드오프).
/// </summary>
public static class PlayerRegistry
{
    private static IDamageable _damageable;
    private static Transform _transform;
    private static Func<bool> _isAliveProbe;

    public static bool HasPlayer => _damageable != null && _transform != null;
    public static Transform Transform => _transform;
    public static IDamageable Damageable => _damageable;
    public static bool IsAlive => _isAliveProbe == null || _isAliveProbe();

    public static void Register(IDamageable damageable, Transform transform, Func<bool> isAliveProbe)
    {
        _damageable = damageable;
        _transform = transform;
        _isAliveProbe = isAliveProbe;
    }

    public static void Unregister(IDamageable damageable)
    {
        if (!ReferenceEquals(_damageable, damageable))
            return;
        _damageable = null;
        _transform = null;
        _isAliveProbe = null;
    }

    /// <summary>
    /// 도메인 리로드 비활성 시 이전 세션의 플레이어 참조가 잔류하는 것을 막는다.
    /// <para>
    /// 특히 <c>_isAliveProbe</c>는 파괴된 <c>PlayerStatComponent</c>를 캡처한 클로저라,
    /// 남아 있으면 스포너의 <c>HasPlayer</c> 게이트가 <b>없는 플레이어를 있다고 답한다</b>.
    /// 새 플레이어가 등록되기 전 한 프레임 동안 적이 유령을 향해 스폰될 수 있다.
    /// </para>
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _damageable = null;
        _transform = null;
        _isAliveProbe = null;
    }
}
