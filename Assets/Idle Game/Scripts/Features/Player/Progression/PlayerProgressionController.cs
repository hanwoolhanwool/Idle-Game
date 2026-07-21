using System;

/// <summary>
/// 경험치 누적과 레벨업을 담당하는 성장 도메인의 핵심.
/// 성장 "규칙"(필요 경험치·레벨별 스탯)은 소유하지 않고 <see cref="PlayerLevelTable"/>에 위임한다.
/// 이 클래스가 아는 것은 "언제 레벨이 오르는가"라는 절차뿐이다(SRP).
/// </summary>
public sealed class PlayerProgressionController : IExpReceiver
{
    private readonly PlayerProgressionState _state;
    private readonly PlayerLevelTable _table;
    private readonly IPlayerBaseStatResolver _resolver;
    private readonly PlayerStatOrchestrator _orchestrator;

    public PlayerProgressionState State => _state;

    /// <summary>최고 레벨에 도달했는가. 도달 시 경험치를 더 이상 받지 않는다.</summary>
    public bool IsMaxLevel => _state.Level >= _table.MaxLevel;

    /// <summary>다음 레벨까지 필요한 경험치. 최고 레벨이면 <see cref="int.MaxValue"/>.</summary>
    public int RequiredExpForNextLevel => _table.RequiredExp(_state.Level);

    public PlayerProgressionController(
        PlayerProgressionConfig config,
        PlayerLevelTable table,
        IPlayerBaseStatResolver resolver,
        PlayerStatOrchestrator orchestrator)
    {
        _table = table != null ? table : throw new ArgumentNullException(nameof(table));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

        // 시작 상태는 config가, 성장 규칙은 table이 소유한다(진실 공급원 분리).
        // 저장된 레벨이 밸런스 패치로 내려간 상한을 넘는 경우를 대비해 클램프한다.
        _state = new PlayerProgressionState
        {
            Level = config != null ? Math.Clamp(config.StartLevel, 1, _table.MaxLevel) : 1,
            Exp = config != null ? Math.Max(0, config.StartExp) : 0,
            PromotionTier = config != null ? config.PromotionTier : 0,
        };
    }

    public void Initialize()
    {
        RefreshBaseStats();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0 || IsMaxLevel)
            return;

        _state.Exp += amount;

        // 한 번의 지급으로 여러 레벨이 오를 수 있다(오프라인 보상·보스 처치).
        while (!IsMaxLevel)
        {
            int required = _table.RequiredExp(_state.Level);
            if (_state.Exp < required)
                break;

            _state.Exp -= required;
            _state.Level++;
        }

        // 최고 레벨에서는 잔여 경험치를 보관하지 않는다(표시·저장 모두 0으로 수렴).
        if (IsMaxLevel)
            _state.Exp = 0;

        RefreshBaseStats();
    }

    /// <summary>현재 성장 상태를 베이스 스탯으로 환산해 StatMachine에 반영한다.</summary>
    public void RefreshBaseStats()
    {
        PlayerBaseStatSet baseStats = _resolver.Resolve(_state);
        _orchestrator.ApplyBaseStats(baseStats);
    }
}
