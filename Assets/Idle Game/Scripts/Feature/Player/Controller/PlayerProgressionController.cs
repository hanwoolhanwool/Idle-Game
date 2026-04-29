public sealed class PlayerProgressionController
{
    private readonly PlayerProgressionState _state;
    private readonly PlayerProgressionConfig _config;
    private readonly IPlayerBaseStatResolver _resolver;
    private readonly PlayerStatOrchestrator _orchestrator;
    
    public PlayerProgressionState State => _state;

    public PlayerProgressionController(
        PlayerProgressionConfig config,
        IPlayerBaseStatResolver resolver,
        PlayerStatOrchestrator orchestrator)
    {
        _config = config;
        _resolver = resolver;
        _orchestrator = orchestrator;

        _state = new PlayerProgressionState()
        {
            Level = config.StartLevel,
            Exp = config.StartExp,
            PromotionTier =  config.PromotionTier,
        };
    }

    public void Initialize()
    {
        RefreshBaseStats();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        _state.Exp += amount;

        while (_state.Exp >= RequiredExpForNextLevel(_state.Level))
        {
            _state.Exp -= RequiredExpForNextLevel(_state.Level);
            _state.Level++;
        }

        RefreshBaseStats();
    }

    public void RefreshBaseStats()
    {
        var baseStats = _resolver.Resolve(_state, _config);
        _orchestrator.ApplyBaseStats(baseStats);
    }

    private int RequiredExpForNextLevel(int currentLevel) => 100 + (currentLevel - 1) * 20;
}