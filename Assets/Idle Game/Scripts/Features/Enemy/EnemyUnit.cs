using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class EnemyUnit : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private int expReward = 10;
    [SerializeField] private int goldReward = 5;

    private float _currentHp;

    public bool IsAlive => _currentHp > 0f;

    /// <summary>
    /// 이 적이 사망으로 사라질 때 자신을 인자로 발행한다. 스포너가 구독해 풀에 반납한다.
    /// EnemyUnit은 구독자(풀·스포너)의 존재를 모른다(DIP) — 사라짐을 방송만 한다.
    /// </summary>
    public event Action<EnemyUnit> Despawned;

    /// <summary>
    /// 스폰 시점에 스테이지 데이터로 이 적을 초기화한다.
    /// 풀에서 재사용되는 인스턴스는 <see cref="Awake"/>가 다시 호출되지 않으므로,
    /// 매 스폰마다 이 메서드가 스탯·보상을 다시 주입하고 체력을 가득 채우는 유일한 리셋 지점이다.
    /// </summary>
    public void Configure(EnemyStat stat, int expReward, int goldReward)
    {
        maxHp = stat.maxHp;
        this.expReward = expReward;
        this.goldReward = goldReward;
        _currentHp = maxHp;
    }

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
        EnemyKillReward.Publish(new KillRewardPayload(expReward, goldReward));

        // 사라짐을 방송한다. 스포너가 이를 듣고 풀에 반납한다(SetActive(false) 전에 알린다).
        Despawned?.Invoke(this);

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