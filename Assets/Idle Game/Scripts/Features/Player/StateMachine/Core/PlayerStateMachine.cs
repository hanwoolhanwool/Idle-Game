using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public sealed class PlayerStateMachine
{
    private readonly Dictionary<PlayerStateID,IPlayerState> _states = new();
    
    public PlayerStateContext Context { get; }
    
    public IPlayerState CurrentState { get; private set; }
    public PlayerStateID CurrentStateID => CurrentState?.StateID ?? PlayerStateID.None;
    public PlayerStateID PreviousStateID { get; private set; } = PlayerStateID.None;
    
    public bool IsInitialized { get; private set; }
    public bool IsTransitioning { get; private set; }
    
    public event Action<PlayerStateID, PlayerStateID> OnStateChanged;

    public PlayerStateMachine(PlayerStateContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void RegisterState(IPlayerState state)
    {
        if( state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.StateID == PlayerStateID.None)
        {
            throw new ArgumentException($"State ID {state.StateID} is invalid.");
        }

        if (!_states.TryAdd(state.StateID, state))
        {
            throw new ArgumentException($"State ID {state.StateID} is already registered.");
        }
    }

    public void RegisterStates(IEnumerable<IPlayerState> states)
    {
        if(states == null)
            throw new ArgumentNullException(nameof(states));
        foreach (var state in states)
        {
            RegisterState(state);
        }
    }

    public void Initialize(PlayerStateID initialStateID)
    {
        if(IsInitialized)
            throw new InvalidOperationException("State machine is already initialized.");
        if(!_states.TryGetValue(initialStateID, out var initialState))
            throw new ArgumentException($"State ID {initialStateID} is invalid.");
        
        CurrentState = initialState;
        PreviousStateID = PlayerStateID.None;
        
        IsInitialized = true;
        CurrentState.Enter();
        
        OnStateChanged?.Invoke(PreviousStateID,CurrentStateID);
    }

    public bool TryChangeState(PlayerStateID nextStateID)
    {
        if (!IsInitialized)
            throw new InvalidImplementationException("StateMachine is not initialized.");
        if (IsTransitioning)
            return false;
        if (CurrentStateID == nextStateID)
            return false;
        if (!_states.TryGetValue(nextStateID, out var nextState))
            throw new KeyNotFoundException($"Target state not found: {nextStateID}");
        
        ChangeStateInternal(nextState);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsInitialized || CurrentState == null)
            return;
        CurrentState.Tick(deltaTime);
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (!IsInitialized || CurrentState == null)
            return;
        CurrentState.FixedTick(fixedDeltaTime);
    }

    private void ChangeStateInternal(IPlayerState nextState)
    {
        IsTransitioning = true;
        
        var prevState = CurrentState;
        var prevStateID = CurrentStateID;
        var nextStateID = nextState?.StateID ?? PlayerStateID.None;

        try
        {
            prevState.Exit();
            CurrentState = nextState;
            
            CurrentState.Enter();
            OnStateChanged?.Invoke(prevStateID, nextStateID);
        }

        finally
        {
            IsTransitioning = false;
        }
    }
}