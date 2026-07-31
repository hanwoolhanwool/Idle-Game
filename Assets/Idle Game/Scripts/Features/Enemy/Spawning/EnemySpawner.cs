using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 데이터에 따라 적을 지속적으로 공급한다. "동시 생존 수"를 목표치로 유지하며,
/// 죽은 적은 풀에 반납해 재사용한다. 무엇을·몇 마리 스폰할지는 <see cref="StageDefinition"/>이
/// 소유하고, 스포너는 이를 주입받아 실행만 한다(SRP).
/// <para>
/// 스테이지 <b>진행</b>(언제 다음으로 넘어가는가)은 모른다 — 그건 <c>StageController</c>의 몫이다.
/// 이 클래스가 하는 일은 "주어진 스테이지를 채우고, 죽으면 알리는 것"뿐이다.
/// </para>
/// </summary>
public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private StageDefinition stage;

    private EnemyPool _pool;
    private float _spawnCooldown;
    private bool _initialFilled;

    /// <summary>
    /// 이 스포너가 내보낸 활성 적. 생존 수를 <c>EnemyRegistry</c>(전역)가 아니라 여기서 세는 이유는,
    /// 전역 목록에는 다른 스포너·보스 등 이 스테이지 정원과 무관한 적이 섞일 수 있기 때문이다.
    /// 회수(<see cref="ClearAlive"/>) 대상도 <b>자기가 내보낸 적으로 한정</b>된다.
    /// </summary>
    private readonly List<EnemyUnit> _active = new();

    /// <summary>
    /// 이 스포너의 적이 <b>처치되어</b> 사라질 때마다 발행된다(강제 회수는 발행하지 않는다).
    /// 진행 컨트롤러가 이를 구독해 클리어 조건을 센다 — 컨트롤러는 적 개체를 전혀 알지 못한다.
    /// </summary>
    public event Action EnemyKilled;

    /// <summary>현재 스폰 중인 스테이지. 배선 검증·디버그용.</summary>
    public StageDefinition CurrentStage => stage;

    private void Start()
    {
        if (stage == null)
        {
            Debug.LogError($"[{name}] StageDefinition이 할당되지 않았습니다. 스폰을 중단합니다.", this);
            enabled = false;
            return;
        }

        SetStage(stage);
    }

    /// <summary>
    /// 스폰 대상 스테이지를 런타임에 교체한다. 진행 컨트롤러의 유일한 제어 진입점이다.
    /// <para>
    /// 교체 시 기존 생존 적을 <b>먼저 회수</b>한다. 이전 배율로 초기화된 적이 남아 있으면
    /// 새 스테이지에 약한 적이 섞여 도는 구간이 생기고, 그 왜곡이 처치율(오프라인 보상의
    /// 입력)까지 오염시킨다.
    /// </para>
    /// </summary>
    public void SetStage(StageDefinition next)
    {
        if (next == null)
        {
            Debug.LogWarning($"[{name}] null 스테이지가 주입되어 무시합니다.", this);
            return;
        }

        ClearAlive();

        bool prefabChanged = _pool == null || _pool.Prefab != next.EnemyPrefab;
        stage = next;

        if (prefabChanged)
        {
            // 프리팹이 바뀌면 기존 재고는 다른 종류의 적이라 재사용할 수 없다.
            // 폐기하지 않으면 스테이지를 오갈 때마다 인스턴스가 씬에 누적된다.
            _pool?.DestroyAll();
            _pool = new EnemyPool(stage.EnemyPrefab, transform, stage.ConcurrentEnemies * 2);
        }

        // 새 스테이지는 정원을 즉시 채운다(전환 직후 빈 무대가 되는 것을 막는다).
        _initialFilled = false;
        _spawnCooldown = 0f;
    }

    /// <summary>
    /// 이 스포너가 내보낸 활성 적을 전원 회수한다. <b>처치가 아니므로</b> 보상도
    /// <see cref="EnemyKilled"/>도 발행되지 않는다 — 전환이 클리어 카운트를 부풀리면 안 된다.
    /// </summary>
    public void ClearAlive()
    {
        // Return이 SetActive(false)를 부르고 그것이 OnDisable→레지스트리 해제로 이어지므로,
        // 순회 중 컬렉션이 흔들리지 않도록 역순으로 비운다.
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = _active[i];
            if (enemy == null)
                continue;

            enemy.Despawned -= OnEnemyDespawned;
            _pool?.Return(enemy);
        }

        _active.Clear();
    }

    private void Update()
    {
        // 싸울 플레이어가 없으면 스폰하지 않는다(빈 무대에 적을 쌓지 않는다).
        // 이 게이트 덕분에 이하의 모든 Spawn 경로는 플레이어 존재를 보장받는다.
        if (!PlayerRegistry.HasPlayer)
            return;

        // 초기 채움: 플레이어가 처음 존재하는 프레임(또는 스테이지 전환 직후)에 목표 수만큼 한 번에 채운다.
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

        if (_active.Count >= stage.ConcurrentEnemies)
            return;

        Spawn();
        _spawnCooldown = stage.SpawnInterval;
    }

    private void FillToTarget()
    {
        int needed = stage.ConcurrentEnemies - _active.Count;
        for (int i = 0; i < needed; i++)
            Spawn();
    }

    private void Spawn()
    {
        EnemyUnit enemy = _pool.Rent();

        // 배율 적용은 스테이지 데이터의 책임이다. 스포너는 계산된 결과를 전달만 한다.
        EnemySpawnParams spawnParams = stage.BuildSpawnParams();
        enemy.Configure(spawnParams);

        enemy.transform.position = RandomSpawnPosition();
        enemy.Despawned += OnEnemyDespawned;
        _active.Add(enemy);
        enemy.gameObject.SetActive(true);
    }

    private void OnEnemyDespawned(EnemyUnit enemy)
    {
        enemy.Despawned -= OnEnemyDespawned;
        _active.Remove(enemy);
        _pool.Return(enemy);

        // Despawned는 사망 경로에서만 오므로 그대로 "처치됨"으로 중계한다.
        EnemyKilled?.Invoke();
    }

    private Vector2 RandomSpawnPosition()
    {
        // Update의 HasPlayer 게이트가 이 메서드 호출 전에 플레이어 존재를 보장하므로,
        // 여기서는 별도 null 폴백 없이 플레이어 위치를 신뢰한다.
        Vector2 center = (Vector2)PlayerRegistry.Transform.position;
        return center + UnityEngine.Random.insideUnitCircle * stage.SpawnRadius;
    }
}
