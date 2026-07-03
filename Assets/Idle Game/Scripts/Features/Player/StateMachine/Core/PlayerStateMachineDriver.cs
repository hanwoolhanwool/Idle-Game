using System;
using UnityEngine;

public sealed class PlayerStateMachineDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour joystickInputReader;
    [SerializeField] private MonoBehaviour playerMovementController;
    [SerializeField] private MonoBehaviour playerAnimationController;
    
    private IJoystickInputReader _inputReader;
    private IPlayerAnimationController _animationController;
    private IPlayerMovementController _movementController;
    
    private PlayerStateMachine _stateMachine;
    
    public PlayerStateMachine StateMachine => _stateMachine;

    private void Awake()
    {
        _inputReader = joystickInputReader as IJoystickInputReader;
        _movementController = playerMovementController as IPlayerMovementController;
        _animationController = playerAnimationController as IPlayerAnimationController;
        
        if(_inputReader == null)
            Debug.LogError($"{name}: joystickInputReader must implement IJoystickInputReader", this);
        if(_animationController == null)
            Debug.LogError($"{name}: animationController must implement IPlayerAnimationController", this);
        if(_movementController == null)
            Debug.LogError($"{name}: movementController must implement IPlayerMovementController", this);
        
        var context = new PlayerStateContext(this.transform, _inputReader, _movementController, _animationController);
        
        _stateMachine = new PlayerStateMachine(context);
        IPlayerState[] states = new IPlayerState[]
        {
            new PlayerState_Idle(_stateMachine),
            new PlayerState_Move(_stateMachine),
            new PlayerState_Attack(_stateMachine),
            new PlayerState_Hit(_stateMachine),
            new PlayerState_Dead(_stateMachine)
        };
        _stateMachine.RegisterStates(states);
        
        _stateMachine.Initialize(PlayerStateID.Idle);
    }

    private void Update()
    {
        _stateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _stateMachine.FixedTick(Time.fixedDeltaTime);
    }
}

