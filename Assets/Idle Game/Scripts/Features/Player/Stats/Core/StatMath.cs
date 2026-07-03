using System.Collections.Generic;

internal static class StatMath
{
    public static float Calculate(
        StatDefinition definition,
        float baseValue,
        List<StatModifier> modifiers)
    {
        if(modifiers == null || modifiers.Count == 0)
            return definition.Clamp(baseValue);
        
        modifiers.Sort(ModifierComparer.Instance);

        float value = baseValue;
        float additiveMulSum = 0f;
        bool hasOverride = false;
        float overridValue = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var statModifier = modifiers[i];
            ref readonly var mod = ref statModifier;

            switch (mod.Op)
            {
                case ModifierOp.Add:
                    value += mod.Value;
                    break;
                case ModifierOp.MultiplyAdditive:
                    additiveMulSum += mod.Value;
                    break;
                case ModifierOp.Multiply:
                    value *= mod.Value;
                    break;
                case ModifierOp.Override:
                    hasOverride = true;
                    overridValue = mod.Value;
                    break;
            }
        }
        value *= (1f + additiveMulSum);
        
        if(hasOverride)
            value = overridValue;
        return definition.Clamp(value);
    }
    
    private sealed class ModifierComparer : IComparer<StatModifier>
    {
        public static readonly ModifierComparer Instance = new();

        public int Compare(StatModifier x, StatModifier y)
        {
            int layerCompare = x.Layer.CompareTo(y.Layer);
            if(layerCompare != 0) return layerCompare;
            
            int orderCompare = x.Order.CompareTo(y.Order);
            if(orderCompare != 0) return orderCompare;
            
            return x.Op.CompareTo(y.Op);
        }
    }
}