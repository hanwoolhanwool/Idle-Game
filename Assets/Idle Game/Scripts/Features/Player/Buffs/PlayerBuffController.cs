using System;
using System.Collections.Generic;
using UnityEngine.Pool;

public sealed class PlayerBuffController
{
    private readonly PlayerStatOrchestrator _orchestrator;
    private readonly Dictionary<string, BuffRuntimeInstance> _activeBuffs = new ();

    public PlayerBuffController(PlayerStatOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0 || _activeBuffs.Count == 0)
            return;

        var expiredKeys = ListPool<string>.Get();

        try
        {
            foreach (var pair in _activeBuffs)
            {
                pair.Value.RemainingTime -= deltaTime;
                if (pair.Value.RemainingTime <= 0f)
                    expiredKeys.Add(pair.Key);
            }

            for (int i = 0; i < expiredKeys.Count; i++)
            {
                string buffId = expiredKeys[i];
                _activeBuffs.Remove(buffId);
                _orchestrator.RemoveBuff(buffId);
            }
        }
        finally
        {
            ListPool<string>.Release(expiredKeys);
        }
    }

    public void Apply(BuffDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.BuffId))
            return;
        if (_activeBuffs.TryGetValue(definition.BuffId, out var existing))
        {
            if(definition.RefreshDurationOnReapply)
                existing.RemainingTime = definition.Duration;
            return;
        }

        var instance = new BuffRuntimeInstance()
        {
            BuffId = definition.BuffId,
            RemainingTime = definition.Duration,
            Modifiers = definition.Modifiers,
            SourceId = $"buff:{definition.BuffId}"
        };
        
        _activeBuffs.Add(definition.BuffId, instance);
        _orchestrator.ApplyBuff(definition);
    }

    public void Remove(string buffId)
    {
        if (!_activeBuffs.Remove(buffId))
            return;
        
        _orchestrator.RemoveBuff(buffId);
    }
}
