using System;
using System.Collections.Generic;

public sealed class StatMachine
{
    private readonly Dictionary<StatType, float> _baseValues = new();
    private readonly Dictionary<StatType, List<StatModifier>> _modifierByStat = new();
    private readonly Dictionary<StatType, float> _finalCache = new();
    private readonly HashSet<StatType> _dirtyStats = new();

    private bool _snapshotDirty = true;
    private StatSnapshot _cachedSnapshot;

    public event Action<StatType, float> OnStatChanged;
    public event Action<StatSnapshot> OnSnapshotChanged;
    
    public StatMachine()
    {
        foreach (var statType in StatCatalog.AllStatTypes)
        {
            _baseValues[statType] = StatCatalog.Get(statType).DefaultBaseValue;
            _modifierByStat[statType] = new List<StatModifier>(8);
            _finalCache[statType] = _baseValues[statType];
            _dirtyStats.Add(statType);
        }
    }

    public float GetBase(StatType statType)
    {
        return _baseValues[statType];
    }
    public float GetFinal(StatType statType)
    {
        RecalculateIfDirty(statType);
        return _finalCache[statType];
    }
    //재공부
    public StatSnapshot GetSnapshot()
    {
        if (_snapshotDirty)
        {
            foreach(var statType in StatCatalog.AllStatTypes)
                RecalculateIfDirty(statType);
            _cachedSnapshot = new StatSnapshot(new Dictionary<StatType, float>(_finalCache));
            _snapshotDirty = false;
        }
        
        return _cachedSnapshot;
    }

    public void UpdateBaseValue(StatType statType, float value)
    {
        var def = StatCatalog.Get(statType);
        var clamped = def.Clamp(value);

        if (NearlyEqual(_baseValues[statType], clamped))
            return;
        _baseValues[statType] = value;
        MarkDirty(statType);
    }

    public void AddModifier(in StatModifier modifier)
    {
        _modifierByStat[modifier.StatType].Add(modifier);
        MarkDirty(modifier.StatType);
    }
    // 재공부
    // list.RemoveAll 부분 완벽 이해 X
    public int RemoveModifiersBySource(object source)
    {
        if (source == null) return 0;
        
        int removed = 0;
        foreach (var pair in _modifierByStat)
        {
            var list = pair.Value;
            int before = list.Count;
            list.RemoveAll(m => ReferenceEquals(m.Source, source));
            int delta = before - list.Count;
            if (delta > 0)
            {
                removed += delta;
                MarkDirty(pair.Key);
            }
        }

        return removed;
    }

    public int RemoveModifierBySourceId(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) return 0;

        int removed = 0;
        foreach (var pair in _modifierByStat)
        {
            var list = pair.Value;
            int before = list.Count;
            list.RemoveAll(m => m.SourceId == sourceId);
            int delta = before - list.Count;
            if (delta > 0)
            {
                removed += delta;
                MarkDirty(pair.Key);
            }
        }

        return removed;
    }

    public void ClearAllModifiers()
    {
        foreach (var pair in _modifierByStat)
        {
            if(pair.Value.Count == 0) continue;
            pair.Value.Clear();
            MarkDirty(pair.Key);
        }
    }

    public IReadOnlyList<StatModifier> GetModifiers(StatType statType)
    {
        return _modifierByStat[statType];
    }

    public void ForceRecalculateAll()
    {
        foreach (var statType in StatCatalog.AllStatTypes)
            _dirtyStats.Add(statType);
        
        _snapshotDirty = true;
        var snapshot = GetSnapshot();
        OnSnapshotChanged?.Invoke(snapshot);
    }

    private void MarkDirty(StatType type)
    {
        _dirtyStats.Add(type);
        _snapshotDirty = true;
    }
    private void RecalculateIfDirty(StatType statType)
    {
        if (!_dirtyStats.Contains(statType))
            return;
        float oldValue = _finalCache[statType];
        float newValue = StatMath.Calculate(
            StatCatalog.Get(statType),
            _baseValues[statType],
            _modifierByStat[statType]);
        
        _finalCache[statType] = newValue;
        _dirtyStats.Remove(statType);
        
        if(!NearlyEqual(oldValue, newValue))
            OnStatChanged?.Invoke(statType, newValue);
    }

    private bool NearlyEqual(float a, float b)
    {
        return Math.Abs(a - b) < 0.0001f;
    }

}