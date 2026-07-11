using UnityEngine;
using UnityEngine.Rendering;

public sealed class EnemyUnit : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private int expReward = 10;

    private float _currentHp;

    public bool IsAlive => _currentHp > 0f;

    public void Awake()
    {
        _currentHp = maxHp;
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || !IsAlive)
            return;

        _currentHp -= damage;
        if (_currentHp <= 0f)
            Die();
    }

    private void Die()
    {
        _currentHp = 0f;

        // "실제 사망" 경로에서만 보상을 발행한다. SetActive(false) 전에 발행해
        // OnDisable(풀링 despawn·씬 언로드)과 분리한다(오지급 방지).
        EnemyKillReward.Publish(expReward);

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EnemyRegistry.Register(this);
    }

    private void OnDisable()
    {
        EnemyRegistry.UnRegister(this);
    }
}