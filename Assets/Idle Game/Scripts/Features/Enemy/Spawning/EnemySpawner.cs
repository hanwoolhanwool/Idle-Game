using UnityEngine;

/// <summary>
/// 스테이지 데이터에 따라 적을 지속적으로 공급한다. "동시 생존 수"를 목표치로 유지하며,
/// 죽은 적은 풀에 반납해 재사용한다. 무엇을·몇 마리 스폰할지는 <see cref="StageDefinition"/>이
/// 소유하고, 스포너는 이를 주입받아 실행만 한다(SRP). (계획서 §6.1)
/// </summary>
public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private StageDefinition stage;

    private EnemyPool _pool;
    private float _spawnCooldown;
    private bool _initialFilled;

    private void Start()
    {
        if (stage == null)
        {
            Debug.LogError($"[{name}] StageDefinition이 할당되지 않았습니다. 스폰을 중단합니다.", this);
            enabled = false;
            return;
        }

        _pool = new EnemyPool(stage.EnemyPrefab, transform, stage.ConcurrentEnemies * 2);
    }

    private void Update()
    {
        // 싸울 플레이어가 없으면 스폰하지 않는다(빈 무대에 적을 쌓지 않는다).
        // 이 게이트 덕분에 이하의 모든 Spawn 경로는 플레이어 존재를 보장받는다.
        if (!PlayerRegistry.HasPlayer)
            return;

        // 초기 채움: 플레이어가 처음 존재하는 프레임에 목표 수만큼 한 번에 채운다.
        if (!_initialFilled)
        {
            FillToTarget();
            _initialFilled = true;
            return;
        }

        // 정상 보충: 죽어서 빈 자리를 SpawnInterval 간격으로 1마리씩 메운다.
        _spawnCooldown -= Time.deltaTime;
        if (_spawnCooldown > 0f)
            return;

        if (EnemyRegistry.All.Count >= stage.ConcurrentEnemies)
            return;

        Spawn();
        _spawnCooldown = stage.SpawnInterval;
    }

    private void FillToTarget()
    {
        int needed = stage.ConcurrentEnemies - EnemyRegistry.All.Count;
        for (int i = 0; i < needed; i++)
            Spawn();
    }

    private void Spawn()
    {
        EnemyUnit enemy = _pool.Rent();
        enemy.Configure(stage.EnemyStat, stage.ExpReward);
        enemy.transform.position = RandomSpawnPosition();
        enemy.Despawned += OnEnemyDespawned;
        enemy.gameObject.SetActive(true);
    }

    private void OnEnemyDespawned(EnemyUnit enemy)
    {
        enemy.Despawned -= OnEnemyDespawned;
        _pool.Return(enemy);
    }

    private Vector2 RandomSpawnPosition()
    {
        // Update의 HasPlayer 게이트가 이 메서드 호출 전에 플레이어 존재를 보장하므로,
        // 여기서는 별도 null 폴백 없이 플레이어 위치를 신뢰한다.
        Vector2 center = (Vector2)PlayerRegistry.Transform.position;
        return center + Random.insideUnitCircle * stage.SpawnRadius;
    }
}
