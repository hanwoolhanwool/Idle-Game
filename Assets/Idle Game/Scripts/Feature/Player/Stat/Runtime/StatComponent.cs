using System;
using System.Collections.Generic;
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
        _currentMp = Stats.GetFinal(StatType.MpRegen);
        Stats.OnStatChanged += HandleStateChanged;
    }

    public void Initialize(PlayerProgressionData progression, IEnumerable<EquipmentRuntimeData> equips)
    {
        
    }

    public void Tick(float deltaTime)
    {
        float hpRegen = Stats.GetFinal(StatType.HpRegen);
        float mpRegen = Stats.GetFinal(StatType.MpRegen);
        float maxHp = Stats.GetFinal(StatType.MaxHp);
        float maxMp = Stats.GetFinal(StatType.MaxMp);
        
        _currentHp = Clamp(_currentHp + hpRegen, 0f, maxHp);
        _currentMp = Clamp(_currentMp + mpRegen, 0f, maxMp);
    }

    public void ApplyDamage(float incomingDamage)
    {
        if (incomingDamage <= 0f) return;
        float defense = Stats.GetFinal(StatType.Defense);
        float damagewReduction = Stats.GetFinal(StatType.DamageReduction);

        float reducedByDefense = incomingDamage * (100f / (100f + Math.Max(0f, defense)));
        float reducedByRate = reducedByDefense * (1f - damagewReduction);
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
    //
    // public float ComputeDps()
    // {
    //     
    // }

    private void HandleStateChanged(StatType arg1, float arg2)
    {
        throw new System.NotImplementedException();
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}