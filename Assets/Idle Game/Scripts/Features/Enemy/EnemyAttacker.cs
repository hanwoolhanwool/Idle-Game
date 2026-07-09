using UnityEngine;

/// <summary>
/// 적이 사거리 안의 플레이어에게 주기적으로 데미지를 주는 근접 공격 behavior.
/// 플레이어는 <see cref="PlayerRegistry"/>로 찾는다. 데미지·간격·사거리는 튜닝 대상.
/// </summary>
[RequireComponent(typeof(EnemyUnit))]
public sealed class EnemyAttacker : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 1f;

    private float _cooldownRemaining;

    private void Update()
    {
        _cooldownRemaining -= Time.deltaTime;
        if (_cooldownRemaining > 0f)
            return;

        if (!PlayerRegistry.HasPlayer || !PlayerRegistry.IsAlive)
            return;

        Vector2 self = transform.position;
        Vector2 toPlayer = (Vector2)PlayerRegistry.Transform.position - self;
        if (toPlayer.sqrMagnitude > attackRange * attackRange)
            return;

        PlayerRegistry.Damageable.ApplyDamage(damage);
        _cooldownRemaining = attackInterval;
    }
}
