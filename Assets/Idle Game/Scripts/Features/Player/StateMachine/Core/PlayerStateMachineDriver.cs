using System;
using UnityEngine;

public sealed class PlayerStateMachineDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour joystickInputReader;
    [SerializeField] private MonoBehaviour playerMovementController;
    [SerializeField] private MonoBehaviour playerAnimationController;
    
    private IMoveInputSource _inputReader;
    private IPlayerAnimationController _animationController;
    private IPlayerMovementController _movementController;
    
    private PlayerStateMachine _stateMachine;
    
    public PlayerStateMachine StateMachine => _stateMachine;

    private void Awake()
    {
        SerializedInterface.TryResolve(joystickInputReader, nameof(joystickInputReader), this, out _inputReader);
        SerializedInterface.TryResolve(playerMovementController, nameof(playerMovementController), this, out _movementController);
        SerializedInterface.TryResolve(playerAnimationController, nameof(playerAnimationController), this, out _animationController);


        var context = new PlayerStateContext(this.transform, _inputReader, _movementController, _animationController);
        
        _stateMachine = new PlayerStateMachine(context);
        IPlayerState[] states = new IPlayerState[]
        {
            new PlayerState_Idle(_stateMachine),
            new PlayerState_Move(_stateMachine),
            new PlayerState_Casting(_stateMachine),
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

