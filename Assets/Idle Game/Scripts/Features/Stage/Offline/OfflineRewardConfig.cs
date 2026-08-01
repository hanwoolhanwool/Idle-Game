using UnityEngine;

/// <summary>
/// 오프라인 보상의 밸런스 손잡이(SO). 재화 배율(M4)과 함께 조정될 값이라
/// 스테이지 카탈로그가 아니라 독립 에셋으로 둔다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Offline Reward Config", fileName = "OfflineRewardConfig")]
public sealed class OfflineRewardConfig : ScriptableObject
{
    [Min(0f)]
    [Tooltip("정산에 반영할 최대 시간(시간 단위). 이보다 오래 비워도 이 시간까지만 계산한다.")]
    public float MaxOfflineHours = 8f;

    [Range(0f, 1f)]
    [Tooltip("온라인 대비 효율. 1이면 접속해 있는 것과 동일한 성과가 된다.")]
    public float OfflineEfficiency = 0.5f;

    /// <summary>
    /// 상한이 없으면 한 달 방치가 최종 콘텐츠를 통째로 건너뛰고, 효율이 1이면
    /// 접속할 이유가 사라진다. 두 값은 그 두 축을 각각 막는다.
    /// </summary>
    public static OfflineRewardConfig CreateDefault()
    {
        var config = CreateInstance<OfflineRewardConfig>();
        config.MaxOfflineHours = 8f;
        config.OfflineEfficiency = 0.5f;
        return config;
    }
}
