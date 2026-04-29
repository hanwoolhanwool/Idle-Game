using UnityEngine;

public sealed class PlayerRoot : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerProgressionConfig ProgressionConfig;
    [SerializeField] private EquipmentDefinition[] startEquipments;
    [SerializeField] private BuffDefinition[] startBuffs;

    [Header("Opthinal Presenters")] 
    [SerializeField] private PlayerHudBinder hudBinder;
    
    private PlayerStatComponent _statComponent;
    private PlayerStatOrchestrator _statOrchestrator;
    
    private PlayerProgressionController _progressionController;
    private PlayerEquipmentController _equipmentController;
    private PlayerBuffController _buffController;
    private PlayerCombatController _combatController;
    private PlayerMovementController _movementController;
    
    public PlayerStatComponent StatComponent => _statComponent;
    public PlayerProgressionController Progression => _progressionController;
    public PlayerEquipmentController Equipment => _equipmentController;
    public PlayerBuffController Buffs => _buffController;
    public PlayerCombatController Combat => _combatController;
    public PlayerMovementController Movement => _movementController;

    private void Awake()
    {
        Compose();
        Initialize();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _statComponent.Tick(dt);
        _buffController.Tick(dt);
        
        // 이동 추가 필요
    }

    [ContextMenu("Apply Test Damage 25")]
    private void ApplyTestDamage()
    {
        _combatController.TakeDamage(25f);
        hudBinder?.RefreshImmediate(_statComponent);
    }

    [ContextMenu("Apply First Start Buff")]
    private void ApplyFirstStartBuff()
    {
        if(startBuffs != null && startBuffs.Length > 0)
            _buffController.Apply(startBuffs[0]);
    }

    [ContextMenu("Gain 120 Exp")]
    private void GainTestExp()
    {
        _progressionController.AddExp(120);
        hudBinder?.RefreshImmediate(_statComponent);
    }

    private void Compose()
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

    private void Initialize()
    {
        _progressionController.Initialize();

        if (_statComponent != null)
        {
            for (int i =0; i < startEquipments.Length; i++)
                _equipmentController.Equip(startEquipments[i]);
        }

        if (startBuffs != null)
        {
            for (int i = 0; i<startBuffs.Length; i++)
                _buffController.Apply(startBuffs[i]);
        }
        
        hudBinder?.Bind(_statComponent);
    }
}