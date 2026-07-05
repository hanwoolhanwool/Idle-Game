using UnityEngine;
public interface IPlayerMovementController
{
    Vector2 MoveInput { get; }
    void SetMovementEnabled(bool enabled);
}