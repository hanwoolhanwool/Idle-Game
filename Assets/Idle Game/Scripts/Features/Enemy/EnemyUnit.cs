using UnityEngine;
using UnityEngine.Rendering;

public sealed class EnemyUnit : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;

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