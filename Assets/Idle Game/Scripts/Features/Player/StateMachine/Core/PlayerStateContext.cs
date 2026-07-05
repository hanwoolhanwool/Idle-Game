using UnityEngine;
public sealed class PlayerStateContext
{
    public Transform Transform { get; }
    public IMoveInputSource MoveInputSource { get; }
    public IPlayerMovementController PlayerMovementController { get; }
    public IPlayerAnimationController PlayerAnimationController { get; }
    
    // 멀티 플레이
    public bool IsOwner { get; set; } = true;
    public bool CanProcessInput => IsOwner && !IsDead && !IsStunned;
    
    public bool IsStunned { get; private set; } = false;
    public bool IsDead { get; private set; } = false;

    public PlayerStateContext
    (Transform transform,
        IMoveInputSource moveInputSource,
        IPlayerMovementController playerMovementController,
        IPlayerAnimationController playerAnimationController)
    {
        Transform = transform;
        MoveInputSource = moveInputSource;
        PlayerMovementController = playerMovementController;
        PlayerAnimationController = playerAnimationController;
    }
    
    public void SetDead(bool value)
    {
        IsDead = value;
    }

    public void SetStunned(bool value)
    {
        IsStunned = value;
    }
}