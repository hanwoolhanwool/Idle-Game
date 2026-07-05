using System.Collections.Generic;
using UnityEngine.Pool;

public sealed class SkillCooldownTracker
{
    private readonly Dictionary<string, float> _remaining = new();
    public bool IsReady(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        return !_remaining.ContainsKey(skillId);
    }

    public void StartCooldown(string skillId, float cooldown)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (cooldown <= 0f)
        {
            _remaining.Remove(skillId);
            return;
        }

        _remaining[skillId] = cooldown;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || _remaining.Count == 0)
            return;
        
        var keys = ListPool<string>.Get();
        try
        {
            keys.AddRange(_remaining.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                string id = keys[i];
                float left = _remaining[id] - deltaTime;

                if (left <= 0f)
                    _remaining.Remove(id);
                else
                    _remaining[id] = left;
            }
        }
        finally
        {
            ListPool<string>.Release(keys);
        }
    }
}