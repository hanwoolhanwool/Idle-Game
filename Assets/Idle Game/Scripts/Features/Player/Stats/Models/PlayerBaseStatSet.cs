using System.Collections.Generic;

/// <summary>
/// 레벨·전직 등 "성장"에서 파생된 베이스 스탯 집합(장비·버프 모디파이어 적용 전).
///
/// <para>
/// <see cref="StatType"/>을 키로 쓰는 이유: 고정 필드(MaxHp, AttackPower, ...)로 두면
/// 새 스탯을 레벨 성장에 참여시킬 때마다 이 클래스와 <see cref="PlayerStatOrchestrator"/>를
/// 함께 고쳐야 한다(OCP 위반). 키 기반이면 <see cref="PlayerLevelTable"/>에 항목을 추가하는
/// 데이터 작업만으로 끝난다.
/// </para>
/// </summary>
public sealed class PlayerBaseStatSet
{
    private readonly Dictionary<StatType, float> _values = new();

    /// <summary>스탯 값을 설정한다(같은 키는 덮어쓴다).</summary>
    public void Set(StatType type, float value) => _values[type] = value;

    /// <summary>설정된 스탯만 순회한다. 여기 없는 스탯은 성장에 참여하지 않는다는 뜻이다.</summary>
    public IReadOnlyDictionary<StatType, float> Entries => _values;

    public bool TryGet(StatType type, out float value) => _values.TryGetValue(type, out value);

    public float GetOrDefault(StatType type, float fallback = 0f)
        => _values.TryGetValue(type, out float value) ? value : fallback;
}
