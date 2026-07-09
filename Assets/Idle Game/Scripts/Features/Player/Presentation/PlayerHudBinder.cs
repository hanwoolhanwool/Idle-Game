using UnityEngine;

/// <summary>
/// 스탯 컴포넌트와 HUD 구현(<see cref="IPlayerHud"/>) 사이의 어댑터.
/// 스탯 변경을 프레임당 1회로 합쳐(K) 초기화·다중 모디파이어 적용 시의 반복 갱신을 제거한다.
/// 실제 표시는 주입된 IPlayerHud에 위임해 로그↔실 UI 교체가 자유롭다(E).
/// </summary>
public sealed class PlayerHudBinder : MonoBehaviour
{
    [Tooltip("실 UI 구현(IPlayerHud). 비워두면 useDebugHudFallback에 따라 로그 HUD를 사용한다.")]
    [SerializeField] private MonoBehaviour hudBehaviour;
    [SerializeField] private bool useDebugHudFallback = true;

    private IPlayerHud _hud;
    private PlayerStatComponent _statComponent;
    private bool _dirty;

    private void Awake()
    {
        _hud = hudBehaviour as IPlayerHud;
        if (_hud == null && hudBehaviour != null)
            Debug.LogWarning($"{name}: hudBehaviour가 IPlayerHud를 구현하지 않습니다.", this);

        if (_hud == null && useDebugHudFallback)
            _hud = new DebugPlayerHud();
    }

    public void Bind(PlayerStatComponent statComponent)
    {
        Unbind();
        _statComponent = statComponent;
        if (_statComponent == null)
            return;

        _statComponent.Stats.OnStatChanged += MarkDirty;
        // 최초 1회 즉시 렌더로 초기 상태 표시.
        RefreshImmediate(_statComponent);
    }

    public void Unbind()
    {
        if (_statComponent == null)
            return;

        _statComponent.Stats.OnStatChanged -= MarkDirty;
        _statComponent = null;
        _dirty = false;
    }

    private void Update()
    {
        if (!_dirty)
            return;
        _dirty = false;
        Render();
    }

    /// <summary>즉시 1회 렌더(디버그 명령·초기화용). 프레임 합치기를 우회한다.</summary>
    public void RefreshImmediate(PlayerStatComponent statComponent)
    {
        if (statComponent == null || _hud == null)
            return;
        _dirty = false;
        Render();
    }

    private void MarkDirty(StatType statType, float value) => _dirty = true;

    private void Render()
    {
        if (_statComponent == null || _hud == null)
            return;

        var stats = _statComponent.Stats;
        var snapshot = new PlayerHudSnapshot(
            _statComponent.CurrentHp, stats.GetFinal(StatType.MaxHp),
            _statComponent.CurrentMp, stats.GetFinal(StatType.MaxMp),
            stats.GetFinal(StatType.AttackPower),
            stats.GetFinal(StatType.AttackSpeed),
            stats.GetFinal(StatType.MoveSpeed),
            _statComponent.ComputeDps());

        _hud.Render(snapshot);
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
