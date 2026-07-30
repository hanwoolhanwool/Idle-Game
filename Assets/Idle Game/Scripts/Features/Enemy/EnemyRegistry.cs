using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 살아 있는 적을 모아 두는 전역 접근점. 타겟 제공자(<c>ITargetProvider</c>)가 여기서 후보를 찾는다.
/// 등록·해제는 <see cref="EnemyUnit"/>의 <c>OnEnable</c>/<c>OnDisable</c>이 담당하므로,
/// 오브젝트 풀의 반납(=비활성화)이 곧 자동 해제가 된다.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<EnemyUnit> _enemies = new();
    public static IReadOnlyList<EnemyUnit> All => _enemies;

    public static void Register(EnemyUnit enemy)
    {
        if (enemy != null && !_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }

    public static void UnRegister(EnemyUnit enemy)
    {
        _enemies.Remove(enemy);
    }

    /// <summary>
    /// 도메인 리로드 비활성("Enter Play Mode Options") 시 이전 세션의 적이 잔류하는 것을 막는다.
    /// <para>
    /// 정적 리스트는 플레이 세션이 끝나도 살아남지만 그 안의 <see cref="EnemyUnit"/>은 파괴된다.
    /// 그대로 두면 다음 세션의 타겟 탐색이 <b>이미 파괴된 유령 적</b>을 골라 공격이 허공에 나간다.
    /// 재현 조건이 특수해 놓치기 쉬운 대신 원인 추적은 매우 어려운 부류라, 진입 시점에 비운다.
    /// (<see cref="EnemyKillReward"/>의 구독자 리셋과 같은 처리)
    /// </para>
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _enemies.Clear();
    }
}
