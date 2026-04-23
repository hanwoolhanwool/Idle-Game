using System.Collections.Generic;
using System.Linq;

public sealed class StatSnapshot
{
    private readonly IReadOnlyDictionary<StatType, float> _stats = new Dictionary<StatType, float>();

    public StatSnapshot(Dictionary<StatType, float> stats)
    {
        _stats = stats;
    }

    public float Get(StatType statType)
    {
        return _stats.TryGetValue(statType, out var value) ? value : 0f;
    }
    
    public IReadOnlyDictionary<StatType, float> AsReadOnly() => _stats;
    public IEnumerable<StatType> AllStatTypes() => _stats.Keys;
}