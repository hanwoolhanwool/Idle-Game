using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class PlayerStatComponent : ITickable
{
    public readonly StatMachine Stats = new();

    private float _currentHp;
    private float _currentMp;
    private bool _isDead;

    public float CurrentHp => _currentHp;
    public float CurrentMp => _currentMp;
    public bool IsDead => _isDead;

    /// <summary>HP가 0에 도달해 사망한 순간 1회 발행된다. (상태머신 Dead 전이·시전 취소 트리거)</summary>
    public event Action OnDied;

    /// <summary>피격했지만 생존한 경우 실제 적용 데미지와 함께 발행된다. (Hit 경직 트리거)</summary>
    public event Action<float> OnDamaged;

    public PlayerStatComponent()
    {
        _currentHp = Stats.GetFinal(StatType.MaxHp);
        _currentMp = Stats.GetFinal(StatType.MaxMp);
        Stats.OnStatChanged += HandleStateChanged;
    }

    public void Tick(float deltaTime)
    {
        float hpRegen = Stats.GetFinal(StatType.HpRegen);
        float mpRegen = Stats.GetFinal(StatType.MpRegen);
        float maxHp = Stats.GetFinal(StatType.MaxHp);
        float maxMp = Stats.GetFinal(StatType.MaxMp);
        
        _currentHp = Clamp(_currentHp + hpRegen * deltaTime, 0f, maxHp);
        _currentMp = Clamp(_currentMp + mpRegen * deltaTime, 0f, maxMp);
    }

    public void ApplyDamage(float incomingDamage)
    {
        if (_isDead) return;
        if (incomingDamage <= 0f) return;
        float defense = Stats.GetFinal(StatType.Defense);
        float damageReduction = Stats.GetFinal(StatType.DamageReduction);

        float reducedByDefense = incomingDamage * (100f / (100f + Math.Max(0f, defense)));
        float reducedByRate = reducedByDefense * (1f - damageReduction);
        _currentHp = Math.Max(0f, _currentHp - reducedByRate);

        if (_currentHp <= 0f)
        {
            _isDead = true;
            OnDied?.Invoke();
        }
        else
        {
            OnDamaged?.Invoke(reducedByRate);
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        _currentHp = Math.Min(_currentHp + amount, Stats.GetFinal(StatType.MaxHp));
    }

    public bool TrySpendMp(float amount)
    {
        if (_currentMp < amount) return false;
        _currentMp -= amount;
        return true;
    }

    public float ComputeFinalDamagePerHit()
    {
        float attack = Stats.GetFinal(StatType.AttackPower);
        float critChance = Stats.GetFinal(StatType.CritChance);
        float critDamage = Stats.GetFinal(StatType.CritDamage);
        
        return attack *((1f - critChance) + (critChance * critDamage));
    }
    
    public float ComputeDps()
    {
        float perHit = ComputeFinalDamagePerHit();
        float attackSpeed = Stats.GetFinal(StatType.AttackSpeed);
        return perHit * attackSpeed;
    }

    /// <summary>
    /// 모든 베이스 스탯/장비/버프 모디파이어가 StatMachine에 적용된 뒤,
    /// 현재 HP/MP를 최종 최대치까지 채운다. (초기화 마지막 단계에서 1회 호출)
    /// </summary>
    public void RefillResourcesToMax()
    {
        _currentHp = Stats.GetFinal(StatType.MaxHp);
        _currentMp = Stats.GetFinal(StatType.MaxMp);
    }

    private void HandleStateChanged(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.MaxHp:
                _currentHp = Math.Min(value, _currentHp);
                break;
            case StatType.MaxMp:
                _currentMp = Math.Min(value, _currentMp);
                break;
        }
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}