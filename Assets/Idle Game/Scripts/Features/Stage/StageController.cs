using System;

/// <summary>
/// 스테이지 <b>진척</b>을 소유한다. 처치 수를 세어 클리어를 판정하고, 다음 스테이지를 스포너에
/// 주입하며, 진행 상태를 세이브에 싣는다. 순수 C#이라 EditMode 테스트에서 그대로 생성된다.
/// <para>
/// 적 개체도 스폰 규칙도 알지 못한다 — 스포너가 발행하는 "한 마리 죽었다"는 사실만 받는다(SRP).
/// 스테이지 목록과 순서는 <see cref="StageCatalog"/>의 책임이다.
/// </para>
/// </summary>
public sealed class StageController : ISaveable, ITickable, IDisposable
{
    /// <summary>처치율 샘플 구간(초). 짧으면 전투 공백 한 번에 값이 크게 흔들린다.</summary>
    private const float SampleWindowSeconds = 5f;

    /// <summary>지수이동평균 평활 계수. 클수록 최근 구간에 민감하다.</summary>
    private const float EmaAlpha = 0.3f;

    private readonly StageCatalog _catalog;
    private readonly EnemySpawner _spawner;
    private readonly IExpReceiver _expReceiver;
    private readonly IGoldReceiver _goldReceiver;
    private readonly OfflineRewardConfig _offlineConfig;

    /// <summary>
    /// 현재 UTC 시각 공급자. 기본은 <c>DateTime.UtcNow</c>지만 테스트에서는 고정 시각을 주입한다 —
    /// 시각을 직접 읽으면 "8시간 비운 뒤 복귀"를 검증할 방법이 사라진다(DIP).
    /// </summary>
    private readonly Func<DateTime> _utcNow;

    private float _windowElapsed;
    private int _windowKills;
    private bool _subscribed;

    public StageDefinition CurrentStage { get; private set; }
    public int KillsInStage { get; private set; }

    /// <summary>최근 실측 처치율(초당). 오프라인 보상 환산의 입력.</summary>
    public float KillsPerSecond { get; private set; }

    /// <summary>현재 스테이지 클리어까지 남은 처치 수. 표시용.</summary>
    public int KillsRemaining =>
        CurrentStage == null ? 0 : Math.Max(0, CurrentStage.KillsToClear - KillsInStage);

    public event Action<StageDefinition> StageChanged;
    public event Action<OfflineReward> OfflineRewardGranted;

    /// <summary>
    /// 즉시 저장이 필요할 때 발행한다(스테이지 전환).
    /// <para>
    /// 컨트롤러가 <c>SaveService</c>를 직접 참조하지 않는 이유: 서비스는 이 컨트롤러를
    /// <see cref="ISaveable"/>로 이미 참조하고 있어, 역참조를 두면 서로 물린다.
    /// 요청만 방송하고 배선은 조립 루트가 결정한다(DIP).
    /// </para>
    /// </summary>
    public event Action SaveRequested;

    public StageController(
        StageCatalog catalog,
        EnemySpawner spawner,
        IExpReceiver expReceiver,
        IGoldReceiver goldReceiver,
        OfflineRewardConfig offlineConfig,
        Func<DateTime> utcNowProvider = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _spawner = spawner;
        _expReceiver = expReceiver;
        _goldReceiver = goldReceiver;
        _offlineConfig = offlineConfig;
        _utcNow = utcNowProvider ?? (() => DateTime.UtcNow);
    }

    /// <summary>스포너 신호를 구독하고 첫 스테이지를 적용한다. 세이브 복원은 이후에 덮어쓴다.</summary>
    public void Initialize()
    {
        if (_spawner != null && !_subscribed)
        {
            _spawner.EnemyKilled += HandleKill;
            _subscribed = true;
        }

        if (CurrentStage == null)
            ApplyStage(_catalog.First());
    }

    public void Dispose()
    {
        if (_spawner != null && _subscribed)
        {
            _spawner.EnemyKilled -= HandleKill;
            _subscribed = false;
        }
    }

    /// <summary>처치율을 구간 평균으로 표집해 지수이동평균으로 갱신한다.</summary>
    public void Tick(float deltaTime)
    {
        _windowElapsed += deltaTime;
        if (_windowElapsed < SampleWindowSeconds)
            return;

        float instant = _windowKills / _windowElapsed;

        // 첫 표본은 평균할 대상이 없으므로 그대로 채택한다. 0에서 서서히 올라오게 두면
        // 짧게 플레이하고 끈 세션의 처치율이 실제보다 한참 낮게 저장된다.
        KillsPerSecond = KillsPerSecond <= 0f
            ? instant
            : KillsPerSecond + (instant - KillsPerSecond) * EmaAlpha;

        _windowElapsed = 0f;
        _windowKills = 0;
    }

    /// <summary>
    /// 적 한 마리가 처치됐음을 알린다. 스포너의 <c>EnemyKilled</c>가 이 메서드를 부른다.
    /// <para>
    /// public인 이유: 처치 소스는 스포너만이 아니다(향후 보스·이벤트 몹). 컨트롤러는
    /// "누가 죽였는지"가 아니라 "한 마리 죽었다"만 알면 되므로 통로를 열어 둔다.
    /// </para>
    /// </summary>
    public void HandleKill()
    {
        KillsInStage++;
        _windowKills++;

        if (CurrentStage == null || KillsInStage < CurrentStage.KillsToClear)
            return;

        AdvanceStage();
    }

    private void AdvanceStage()
    {
        StageDefinition next = _catalog.Next(CurrentStage);
        KillsInStage = 0;

        // 마지막 스테이지에서는 전환하지 않고 카운트만 되감아 무한 반복한다.
        // "더 갈 곳 없음"을 막다른 길로 만들면 방치가 무의미해지기 때문이다(M0의 단일 스테이지 동작과 같다).
        if (next == null)
            return;

        ApplyStage(next);

        // 전환 직후 강제 종료되면 진척이 통째로 날아간다. 주기 저장만 믿지 않는다.
        SaveRequested?.Invoke();
    }

    private void ApplyStage(StageDefinition stage)
    {
        if (stage == null)
            return;

        CurrentStage = stage;
        _spawner?.SetStage(stage);
        StageChanged?.Invoke(stage);
    }

    public void CaptureState(PlayerSaveData data)
    {
        if (data?.World == null)
            return;

        data.World.StageId = CurrentStage != null ? CurrentStage.StageId : string.Empty;
        data.World.KillsInStage = KillsInStage;
        data.World.KillsPerSecond = KillsPerSecond;
        data.World.LastSaveUtcTicks = _utcNow().Ticks;
    }

    public void RestoreState(PlayerSaveData data)
    {
        if (data?.World == null)
            return;

        // 밸런스 패치로 삭제된 스테이지를 가리키는 세이브가 있을 수 있다. 첫 스테이지로 폴백한다.
        StageDefinition restored = _catalog.FindById(data.World.StageId) ?? _catalog.First();

        KillsInStage = Math.Max(0, data.World.KillsInStage);
        KillsPerSecond = Math.Max(0f, data.World.KillsPerSecond);

        ApplyStage(restored);
        GrantOfflineReward(data.World.LastSaveUtcTicks);
    }

    /// <summary>
    /// 자리를 비운 동안의 성과를 <b>기존 보상 경로</b>로 지급한다.
    /// <para>
    /// 별도 경로를 만들지 않는 이유: <see cref="IExpReceiver.AddExp"/>에 넣으면 다중 레벨업 처리와
    /// 변경 알림이 공짜로 따라온다. 온라인·오프라인이 두 벌의 지급 로직을 갖지 않는다.
    /// </para>
    /// <para>
    /// 처치 수는 <b>클리어 카운트에 더하지 않는다.</b> 오프라인 전환을 허용하면 방치만으로
    /// 최종 스테이지에 도달할 수 있어, "도중에 최소 한 번 벽에 막힌다"는 M1 목표와 충돌한다.
    /// </para>
    /// </summary>
    private void GrantOfflineReward(long lastSaveUtcTicks)
    {
        // 기준선 없음(첫 실행) 또는 손상된 값. 예외 대신 조용히 건너뛴다.
        if (lastSaveUtcTicks <= 0L || lastSaveUtcTicks > DateTime.MaxValue.Ticks)
            return;

        var lastSave = new DateTime(lastSaveUtcTicks, DateTimeKind.Utc);
        OfflineReward reward = OfflineRewardCalculator.Calculate(
            _utcNow() - lastSave, KillsPerSecond, CurrentStage, _offlineConfig);

        if (!reward.HasReward)
            return;

        _expReceiver?.AddExp(reward.Exp);
        _goldReceiver?.AddGold(reward.Gold);
        OfflineRewardGranted?.Invoke(reward);
    }
}
