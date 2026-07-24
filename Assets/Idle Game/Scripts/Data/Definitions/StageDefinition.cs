using UnityEngine;

/// <summary>
/// 한 스테이지의 스폰 규칙·보상을 담는 데이터 그릇(SO). 로직은 없다.
/// 스포너는 이 값을 주입받을 뿐 스테이지 내용을 모르므로(SRP), 스테이지 추가가
/// 에셋 생성 1건으로 끝난다(OCP). (계획서: docs/design/m0-close-the-loop-plan.md §5.2)
/// </summary>
[CreateAssetMenu(menuName = "Game/Stage Definition", fileName = "StageDefinition")]
public sealed class StageDefinition : ScriptableObject
{
    [Header("Enemy")]
    [Tooltip("스폰할 적 프리팹.")]
    public EnemyUnit EnemyPrefab;

    [Tooltip("적 스탯 데이터(체력·공격력 등).")]
    public EnemyStat EnemyStat;

    [Header("Spawning")]
    [Min(1)]
    [Tooltip("동시에 살아 있게 유지할 적의 목표 수.")]
    public int ConcurrentEnemies = 5;

    [Min(0.1f)]
    [Tooltip("한 마리를 스폰한 뒤 다음 스폰까지 기다리는 시간(초).")]
    public float SpawnInterval = 1f;

    [Min(0f)]
    [Tooltip("플레이어 주변 이 반경 안의 랜덤 위치에 적을 배치한다.")]
    public float SpawnRadius = 8f;

    [Header("Reward")]
    [Min(0)]
    [Tooltip("적 한 마리 처치 시 지급할 경험치.")]
    public int ExpReward = 10;
}
