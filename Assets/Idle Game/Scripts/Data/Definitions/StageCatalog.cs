using UnityEngine;

/// <summary>
/// 스테이지의 <b>목록과 순서</b>를 소유하는 SO. 개별 스테이지는 자기 순서를 모르고,
/// 진행 컨트롤러는 목록의 구성을 모른다(SRP).
/// <para>
/// 스테이지 추가는 에셋 1건 생성 + 이 배열에 항목 추가로 끝난다. 코드는 바뀌지 않는다(OCP).
/// </para>
/// </summary>
[CreateAssetMenu(menuName = "Game/Stage Catalog", fileName = "StageCatalog")]
public sealed class StageCatalog : ScriptableObject
{
    [Tooltip("진행 순서대로 나열한다. 배열 순서가 곧 스테이지 순서다.")]
    public StageDefinition[] Stages;

    public bool IsEmpty => Stages == null || Stages.Length == 0;

    /// <summary>첫 스테이지. 목록이 비어 있으면 <c>null</c>.</summary>
    public StageDefinition First() => IsEmpty ? null : Stages[0];

    /// <summary>
    /// 식별자로 스테이지를 찾는다. 없으면 <c>null</c> — 밸런스 패치로 삭제된 스테이지를
    /// 가리키는 세이브가 로드될 수 있으므로, 호출부가 폴백을 결정한다.
    /// </summary>
    public StageDefinition FindById(string stageId)
    {
        if (IsEmpty || string.IsNullOrEmpty(stageId))
            return null;

        for (int i = 0; i < Stages.Length; i++)
        {
            if (Stages[i] != null && Stages[i].StageId == stageId)
                return Stages[i];
        }

        return null;
    }

    /// <summary>
    /// 다음 스테이지. 마지막이거나 목록에 없으면 <c>null</c>(= 더 진행할 곳 없음).
    /// </summary>
    public StageDefinition Next(StageDefinition current)
    {
        if (IsEmpty || current == null)
            return null;

        for (int i = 0; i < Stages.Length - 1; i++)
        {
            if (Stages[i] == current)
                return Stages[i + 1];
        }

        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 식별자 중복·누락을 에디터에서 잡는다. 중복된 StageId는 세이브 복원 시
    /// 엉뚱한 스테이지로 이어지는데, 런타임에는 증상이 "가끔 진행이 튄다"로만 보여 추적이 어렵다.
    /// </summary>
    private void OnValidate()
    {
        if (IsEmpty)
            return;

        for (int i = 0; i < Stages.Length; i++)
        {
            if (Stages[i] == null)
            {
                Debug.LogWarning($"[{name}] Stages[{i}]가 비어 있습니다.", this);
                continue;
            }

            if (string.IsNullOrWhiteSpace(Stages[i].StageId))
            {
                Debug.LogWarning($"[{name}] Stages[{i}]({Stages[i].name})의 StageId가 비어 있습니다.", this);
                continue;
            }

            for (int j = i + 1; j < Stages.Length; j++)
            {
                if (Stages[j] != null && Stages[j].StageId == Stages[i].StageId)
                    Debug.LogWarning($"[{name}] StageId '{Stages[i].StageId}'가 중복됩니다({i}, {j}).", this);
            }
        }
    }
#endif
}
