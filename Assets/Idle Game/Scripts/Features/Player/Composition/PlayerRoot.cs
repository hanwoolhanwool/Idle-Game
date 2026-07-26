using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 오브젝트 그래프의 조립 루트(Composition Root).
/// 직렬화된 참조를 받아 컨트롤러들을 조립하고, 생명주기(Start/Update)에서
/// ITickable 목록을 순회하는 얇은 글루 역할만 담당한다.
/// 조립은 driver.StateMachine이 준비되는 Start에서 단일 경로로 수행한다.
/// </summary>
public sealed class PlayerRoot : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerProgressionConfig ProgressionConfig;
    [Tooltip("레벨 성장 규칙(필요 경험치·레벨별 베이스 스탯)의 단일 출처. 필수.")]
    [SerializeField] private PlayerLevelTable levelTable;
    [SerializeField] private EquipmentDefinition[] startEquipments;
    [SerializeField] private BuffDefinition[] startBuffs;

    [Header("Optional Presenters")]
    [SerializeField] private PlayerHudBinder hudBinder;

    [Header("Skills")]
    [SerializeField] private PlayerStateMachineDriver stateMachineDriver;
    [SerializeField] private MonoBehaviour movementBehaviour;
    [SerializeField] private SkillLoadoutConfig loadoutConfig;
    [SerializeField] private AutoBattleInputSource autoBattle;
    [SerializeField] private SkillButton[] skillButtons;

    [Header("Input / Control Mode")]
    [Tooltip("능동(조이스틱) 입력 소스. IMoveInputSource를 구현해야 한다.")]
    [SerializeField] private MonoBehaviour activeInputSource;
    [SerializeField] private PlayerControlMode initialControlMode = PlayerControlMode.Active;

    private PlayerStatComponent _statComponent;
    private PlayerStatOrchestrator _statOrchestrator;

    private PlayerProgressionController _progressionController;
    private PlayerEquipmentController _equipmentController;
    private PlayerBuffController _buffController;
    private PlayerCombatController _combatController;
    private PlayerSkillController _skillController;
    private AutoCastController _autoCast;
    private PlayerInputRouter _inputRouter;
    private PlayerDeathHandler _deathHandler;
    private PlayerHitReaction _hitReaction;
    private EnemyExpRewardHandler _expRewardHandler;
    private PlayerWallet _wallet;
    private EnemyGoldRewardHandler _goldRewardHandler;

    private readonly List<ITickable> _tickables = new();

    private void Start()
    {
        if (!Compose())
            return;

        Initialize();
        RegisterTickables();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _tickables.Count; i++)
            _tickables[i].Tick(dt);
    }

    private void OnDestroy()
    {
        _deathHandler?.Dispose();
        _hitReaction?.Dispose();
        _expRewardHandler?.Dispose();
        _goldRewardHandler?.Dispose();

        if (_combatController != null)
            PlayerRegistry.Unregister(_combatController);
    }

    /// <summary>코어 조립에 실패하면(필수 참조 누락) 이후 단계를 진행하지 않는다.</summary>
    private bool Compose()
    {
        if (!ComposeCore())
            return false;

        ComposeSkills();
        return true;
    }

    private bool ComposeCore()
    {
        if (levelTable == null)
        {
            Debug.LogError("PlayerRoot: levelTable이 연결되지 않았습니다. 레벨 성장이 불가능하므로 조립을 중단합니다.", this);
            return false;
        }

        _statComponent = new PlayerStatComponent();
        _statOrchestrator = new PlayerStatOrchestrator(_statComponent);
        _progressionController = new PlayerProgressionController(
            ProgressionConfig,
            levelTable,
            new PlayerBaseStatResolver(levelTable),
            _statOrchestrator);
        _equipmentController = new PlayerEquipmentController(_statOrchestrator);
        _buffController = new PlayerBuffController(_statOrchestrator);
        _combatController = new PlayerCombatController(_statComponent);

        // 적 처치 보상 → 경험치 배선. 적↔성장의 유일한 결합점(브리지)을 여기서만 안다.
        _expRewardHandler = new EnemyExpRewardHandler(_progressionController);

        // 적 처치 보상 → 골드 배선. 지갑을 소유하고 브리지로 허브에 연결한다(경험치와 대칭).
        _wallet = new PlayerWallet();
        _goldRewardHandler = new EnemyGoldRewardHandler(_wallet);
        return true;
    }

    private void ComposeSkills()
    {
        if (stateMachineDriver == null)
        {
            Debug.LogError("PlayerRoot: stateMachineDriver 연결이 없습니다.", this);
            return;
        }

        if (!SerializedInterface.TryResolve(movementBehaviour, nameof(movementBehaviour), this, out IPlayerMovementController movement))
            return;

        // 이동 속도의 단일 출처 배선: 이동이 SO가 아닌 StatMachine을 읽도록 스탯을 주입.
        if (movementBehaviour is IStatDrivenMovement statDrivenMovement)
            statDrivenMovement.BindStats(_statComponent.Stats);
        else
            Debug.LogWarning("PlayerRoot: movementBehaviour가 IStatDrivenMovement를 구현하지 않아 스탯 기반 이동이 비활성화됩니다.", this);

        ComposeInputRouter(movementBehaviour);

        if (loadoutConfig == null)
        {
            Debug.LogError("PlayerRoot: loadoutConfig가 연결되지 않았습니다.", this);
            return;
        }

        var loadout = new SkillLoadout(loadoutConfig.BasicAttack);
        SkillDefinition[] equipped = loadoutConfig.EquippedSkills;
        if (equipped != null)
        {
            for (int i = 0; i < equipped.Length; i++)
                loadout.TryEquip(i + 1, equipped[i]);
        }

        var cooldownTracker = new SkillCooldownTracker();
        // 시전 전용 Casting 상태로 진입/복귀. 평타(Attack)와 스킬 시전을 구분한다(§3-D).
        var castGate = new PlayerStateMachineCastGate(
            stateMachineDriver.StateMachine,
            castStateID: PlayerStateID.Casting,
            returnStateID: PlayerStateID.Idle);

        _skillController = new PlayerSkillController(
            loadout,
            cooldownTracker,
            _combatController,
            _statComponent,
            _buffController,
            movement,
            castGate);

        if (autoBattle != null)
        {
            var targetProvider = new NearestEnemyTargetProvider();
            autoBattle.Initialize(targetProvider);
            _autoCast = new AutoCastController(_skillController, autoBattle);
        }

        if (skillButtons != null)
        {
            for (int i = 0; i < skillButtons.Length; i++)
                skillButtons[i]?.Bind(_skillController);
        }

        // 사망/피격 파이프라인: 자원 컴포넌트의 사망 이벤트를 상태머신·스킬·이동과 연동.
        _deathHandler = new PlayerDeathHandler(
            _statComponent,
            stateMachineDriver.StateMachine,
            _skillController,
            movement);

        // 피격(생존) 시 Hit(경직) 전이.
        _hitReaction = new PlayerHitReaction(_statComponent, stateMachineDriver.StateMachine);
    }

    /// <summary>
    /// 입력 라우터를 조립해 이동 컨트롤러에 주입한다.
    /// 능동(조이스틱)/방치(자동전투) 소스를 감싸 런타임에 스왑 가능하게 한다.
    /// 능동 소스가 미배선이면 라우터를 주입하지 않아, 이동은 자신의 직렬화 소스를 그대로 쓴다(동작 보존).
    /// </summary>
    private void ComposeInputRouter(MonoBehaviour movement)
    {
        var activeSource = activeInputSource as IMoveInputSource;
        if (activeSource == null)
        {
            if (activeInputSource != null)
                Debug.LogWarning("PlayerRoot: activeInputSource가 IMoveInputSource를 구현하지 않습니다. 입력 라우터를 건너뜁니다.", this);
            return;
        }

        var idleSource = autoBattle as IMoveInputSource;
        _inputRouter = new PlayerInputRouter(activeSource, idleSource, initialControlMode);

        if (movement is IMoveInputConsumer inputConsumer)
            inputConsumer.SetInputSource(_inputRouter);
        else
            Debug.LogWarning("PlayerRoot: movementBehaviour가 IMoveInputConsumer를 구현하지 않아 모드 전환이 이동에 반영되지 않습니다.", this);
    }

    /// <summary>방치↔능동 제어 모드를 런타임에 전환한다.</summary>
    public void SetControlMode(PlayerControlMode mode)
    {
        _inputRouter?.SetMode(mode);
    }

    /// <summary>현재 제어 모드(라우터 미구성 시 null).</summary>
    public PlayerControlMode? ControlMode => _inputRouter?.Mode;

    private void Initialize()
    {
        _progressionController.Initialize();

        if (startEquipments != null)
        {
            _equipmentController.Initialize(startEquipments);
        }

        if (startBuffs != null)
        {
            for (int i = 0; i < startBuffs.Length; i++)
                _buffController.Apply(startBuffs[i]);
        }

        // 베이스 스탯 → 장비 → 버프가 모두 StatMachine에 적용된 뒤,
        // 최종 MaxHp/MaxMp 기준으로 현재 자원을 가득 채운다.
        _statComponent.RefillResourcesToMax();
        hudBinder?.Bind(_statComponent);

        // 적이 플레이어를 피격 대상으로 찾을 수 있도록 등록(피격 소스 일원화).
        PlayerRegistry.Register(_combatController, transform, () => !_statComponent.IsDead);
    }

    private void RegisterTickables()
    {
        // 등록 순서 = 기존 Update 실행 순서(stat → buff → skill → autoCast) 유지.
        _tickables.Add(_statComponent);
        _tickables.Add(_buffController);
        if (_skillController != null)
            _tickables.Add(_skillController);
        if (_autoCast != null)
            _tickables.Add(_autoCast);
    }

#if UNITY_EDITOR
    // 에디터 전용 디버그 훅. PlayerDebugCommands가 호출하며, 빌드에는 포함되지 않는다.
    internal void DebugApplyDamage(float damage)
    {
        _combatController.ApplyDamage(damage);
        hudBinder?.RefreshImmediate(_statComponent);
    }

    internal void DebugApplyFirstStartBuff()
    {
        if (startBuffs != null && startBuffs.Length > 0)
            _buffController.Apply(startBuffs[0]);
    }

    internal void DebugGainExp(int amount)
    {
        _progressionController.AddExp(amount);
        hudBinder?.RefreshImmediate(_statComponent);
    }

    internal void DebugToggleControlMode()
    {
        if (_inputRouter == null)
        {
            Debug.LogWarning("PlayerRoot: 입력 라우터가 구성되지 않아 모드 전환 불가(activeInputSource 미배선).", this);
            return;
        }

        _inputRouter.ToggleMode();
        Debug.Log($"[PlayerRoot] 제어 모드 → {_inputRouter.Mode}", this);
    }
#endif
}
