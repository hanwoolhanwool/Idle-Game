using UnityEngine;

public sealed class NearestEnemyTargetProvider : ITargetProvider
{
    public Transform GetNearestTarget(Vector2 from)
    {
        Transform nearest = null;
        float minSqr = float.MaxValue;
        var enemies = EnemyRegistry.All;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;
            
            float sqr = ((Vector2)enemy.transform.position - from).sqrMagnitude;
            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = enemy.transform;
            }
        }
        return nearest;
    }
}