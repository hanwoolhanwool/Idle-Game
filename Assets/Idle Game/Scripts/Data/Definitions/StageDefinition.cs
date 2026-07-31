using UnityEngine;

/// <summary>
/// 한 스테이지의 스폰 규칙·난이도·목표·보상을 담는 데이터 그릇(SO). 로직은 없다.
/// 스포너는 이 값을 주입받을 뿐 스테이지 내용을 모르므로(SRP), 스테이지 추가가
/// 에셋 생성 1건으로 끝난다(OCP). (계획서: docs/design/m1-vertical-slice-plan.md §5.1)
/// <para>
/// 이 SO는 자기가 <b>몇 번째인지, 다음이 무엇인지 모른다</b>. 순서는 <see cref="StageCatalog"/>의
/// 책임이다 — 스테이지가 다음 스테이지를 직접 참조하면 중간 삽입·순서 변경 때마다
/// 에셋들을 연쇄 수정해야 한다.
/// </para>
/// </summary>
[CreateAssetMenu(menuName = "Game/Stage Definition", fileName = "StageDefinition")]
public sealed class StageDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("세이브에 기록되는 안정 식별자. 배열 순서가 바뀌어도 저장된 진행이 밀리지 않도록 문자열로 둔다.")]
    public string StageId = "stage_01";

    [Header("Enemy")]
    [Tooltip("스폰할 적 프리팹.")]
    public EnemyUnit EnemyPrefab;

    [Tooltip("적 스탯 데이터(체력 등)의 기준값. 실제 적용값은 아래 배율이 곱해진다.")]
    public EnemyStat EnemyStat;

    [Min(0.01f)]
    [Tooltip("적 스탯에 곱할 난이도 배율. 뒤 스테이지일수록 크게 둔다.")]
    public float EnemyStatMultiplier = 1f;

    [Header("Clear Condition")]
    [Min(1)]
    [Tooltip("이 스테이지를 클리어하는 데 필요한 누적 처치 수.")]
    public int KillsToClear = 20;

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

    [Min(0)]
    [Tooltip("적 한 마리 처치 시 지급할 골드.")]
    public int GoldReward = 5;

    /// <summary>
    /// 난이도 배율이 반영된 적 스폰 파라미터. 배율 적용을 여기서 하는 이유는
    /// 계산 규칙이 스테이지 데이터에 속하기 때문이다 — 적도 스포너도 배율의 존재를 모른다.
    /// </summary>
    public EnemySpawnParams BuildSpawnParams()
    {
        float baseHp = EnemyStat != null ? EnemyStat.maxHp : 1f;
        return new EnemySpawnParams(baseHp * EnemyStatMultiplier, ExpReward, GoldReward);
    }
}
