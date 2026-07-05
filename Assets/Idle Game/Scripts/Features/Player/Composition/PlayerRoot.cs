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
    [SerializeField] private EquipmentDefinition[] startEquipments;
    [SerializeField] private BuffDefinition[] startBuffs;

    [Header("Optional Presenters")]
    [SerializeField] private PlayerHudBinder hudBinder;

    [Header("Skills")]
    [SerializeField] private PlayerStateMachineDriver stateMachineDriver;
    [SerializeField] private MonoBehaviour movementBehaviour;
    [SerializeField] private SkillDefinition basicAttack;
    [SerializeField] private SkillDefinition[] equippedSkills;
    [SerializeField] private AutoBattleInputSource autoBattle;
    [SerializeField] private SkillButton[] skillButtons;

    private PlayerStatComponent _statComponent;
    private PlayerStatOrchestrator _statOrchestrator;

    private PlayerProgressionController _progressionController;
    private PlayerEquipmentController _equipmentController;
    private PlayerBuffController _buffController;
    private PlayerCombatController _combatController;
    private PlayerSkillController _skillController;
    private AutoCastController _autoCast;

    private readonly List<ITickable> _tickables = new();

    private void Start()
    {
        Compose();
        Initialize();
        RegisterTickables();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _tickables.Count; i++)
            _tickables[i].Tick(dt);
    }

    private void Compose()
    {
        ComposeCore();
        ComposeSkills();
    }

    private void ComposeCore()
    {
        _statComponent = new PlayerStatComponent();
        _statOrchestrator = new PlayerStatOrchestrator(_statComponent);
        _progressionController = new PlayerProgressionController(
            ProgressionConfig,
            new PlayerBaseStatResolver(),
            _statOrchestrator);
        _equipmentController = new PlayerEquipmentController(_statOrchestrator);
        _buffController = new PlayerBuffController(_statOrchestrator);
        _combatController = new PlayerCombatController(_statComponent);
    }

    private void ComposeSkills()
    {
        var movement = movementBehaviour as IPlayerMovementController;
        if (stateMachineDriver == null || movement == null)
        {
            Debug.LogError("PlayerRoot: stateMachineDriver 또는 movementBehaviour 연결이 잘못되었습니다.", this);
            return;
        }

        var loadout = new SkillLoadout(basicAttack);
        if (equippedSkills != null)
        {
            for (int i = 0; i < equippedSkills.Length; i++)
                loadout.TryEquip(i + 1, equippedSkills[i]);
        }

        var cooldownTracker = new SkillCooldownTracker();
        var castGate = new PlayerStateMachineCastGate(stateMachineDriver.StateMachine);

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
    }

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
        _combatController.TakeDamage(damage);
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
#endif
}
