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

    #region buff
    // 버프는 반드시 리펙터링을 거쳐야한다.
    public void ApplyTimeBuff(TimedBuffData buffData)
    {
        foreach (var modifier in buffData.Modifiers)
            Stats.AddModifier(modifier);
    }

    public void RemoveTimeBuffByBuffId(TimedBuffData buffData)
    {
        Stats.RemoveModifierBySourceId(buffData.BuffId);
    }
    #endregion

    private void ApplyProgression(PlayerProgressionData progression)
    {
        Stats.UpdateBaseValue(StatType.MaxHp, progression.BaseHp);
        Stats.UpdateBaseValue(StatType.MpRegen, progression.BaseMp);
        Stats.UpdateBaseValue(StatType.AttackPower, progression.BaseAttakPower);
        Stats.UpdateBaseValue(StatType.AttackSpeed, progression.BaseAttakSpeed);
        Stats.UpdateBaseValue(StatType.MoveSpeed, progression.BaseMoveSpeed);
        Stats.UpdateBaseValue(StatType.Defense, progression.BaseDefence);
    }
    // 재공부
    // 리펙터링 필수
    // 현재 장비에 대한 구분이 존재하지 않는다. 따라서 장비를 구분해주는 Machin 또는 controller가 필요한상태
    private void ApplyEquipments(IEnumerable<EquipmentRuntimeData> equipments)
    {
        if (equipments == null) return;
        foreach (var equipment in equipments)
        {
            foreach (var modifier in equipment.Modifiers)
                Stats.AddModifier(modifier);
        }
    }

    private void RefillResourceToCapOnInitialize()
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