using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class PlayerStatComponent
{
    public readonly StatMachine Stats = new();

    private float _currentHp;
    private float _currentMp;
    
    public float CurrentHp => _currentHp;
    public float CurrentMp => _currentMp;

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
        if (incomingDamage <= 0f) return;
        float defense = Stats.GetFinal(StatType.Defense);
        float damageReduction = Stats.GetFinal(StatType.DamageReduction);

        float reducedByDefense = incomingDamage * (100f / (100f + Math.Max(0f, defense)));
        float reducedByRate = reducedByDefense * (1f - damageReduction);
        _currentHp = Math.Max(0f, _currentHp - reducedByRate);
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