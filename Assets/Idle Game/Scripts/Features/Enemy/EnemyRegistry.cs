using System.Collections.Generic;

public static class EnemyRegistry
{
    private static readonly List<EnemyUnit> _enemies = new();
    public static IReadOnlyList<EnemyUnit> All => _enemies;

    public static void Register(EnemyUnit enemy)
    {
        if (enemy != null && !_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }

    public static void UnRegister(EnemyUnit enemy)
    {
        _enemies.Remove(enemy);
    }
}