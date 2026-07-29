using UnityEngine;

/// <summary>
/// 스탯·성장·재화와 HUD 구현(<see cref="IPlayerHud"/>) 사이의 어댑터.
/// 세 출처의 변경을 프레임당 1회로 합쳐(K) 초기화·다중 모디파이어 적용 시의 반복 갱신을 제거한다.
/// 실제 표시는 주입된 IPlayerHud에 위임해 로그↔실 UI 교체가 자유롭다(E).
/// </summary>
public sealed class PlayerHudBinder : MonoBehaviour
{
    [Tooltip("실 UI 구현(IPlayerHud). 비워두면 useDebugHudFallback에 따라 로그 HUD를 사용한다.")]
    [SerializeField] private MonoBehaviour hudBehaviour;
    [SerializeField] private bool useDebugHudFallback = true;

    private IPlayerHud _hud;
    private PlayerStatComponent _statComponent;
    private PlayerProgressionController _progression;
    private PlayerWallet _wallet;
    private bool _dirty;

    private void Awake()
    {
        _hud = hudBehaviour as IPlayerHud;
        if (_hud == null && hudBehaviour != null)
            Debug.LogWarning($"{name}: hudBehaviour가 IPlayerHud를 구현하지 않습니다.", this);

        if (_hud == null && useDebugHudFallback)
            _hud = new DebugPlayerHud();
    }

    /// <summary>
    /// 표시 대상을 연결한다. 성장·재화는 선택이며(null 허용), 없으면 해당 항목이 기본값으로 표시된다 —
    /// 조립이 부분적으로만 성공해도 HUD가 죽지 않게 하기 위함이다.
    /// </summary>
    public void Bind(
        PlayerStatComponent statComponent,
        PlayerProgressionController progression = null,
        PlayerWallet wallet = null)
    {
        Unbind();

        _statComponent = statComponent;
        _progression = progression;
        _wallet = wallet;

        if (_statComponent == null)
            return;

        _statComponent.Stats.OnStatChanged += MarkDirty;

        // 골드·경험치는 스탯이 아니라서 OnStatChanged로는 잡히지 않는다. 각 축을 따로 구독한다.
        if (_progression != null)
            _progression.ProgressChanged += MarkDirty;

        if (_wallet != null)
            _wallet.GoldChanged += MarkDirty;

        // 최초 1회 즉시 렌더로 초기 상태 표시.
        RefreshImmediate(_statComponent);
    }

    public void Unbind()
    {
        if (_progression != null)
        {
            _progression.ProgressChanged -= MarkDirty;
            _progression = null;
        }

        if (_wallet != null)
        {
            _wallet.GoldChanged -= MarkDirty;
            _wallet = null;
        }

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

    // 세 출처가 각기 다른 시그니처로 알려 오므로 오버로드로 받아 하나의 더티 플래그로 합친다.
    private void MarkDirty(StatType statType, float value) => _dirty = true;
    private void MarkDirty(long gold) => _dirty = true;
    private void MarkDirty() => _dirty = true;

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
            _statComponent.ComputeDps(),
            _progression?.State.Level ?? 1,
            _progression?.State.Exp ?? 0,
            _progression?.RequiredExpForNextLevel ?? 0,
            _wallet?.Gold ?? 0L);

        _hud.Render(snapshot);
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
