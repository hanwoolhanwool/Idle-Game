using UnityEngine;
using System;
using UnityEngine.InputSystem;
[CreateAssetMenu(fileName = "InputReader", menuName = "Idle Game/Input Reader")]
public class InputReader : ScriptableObject,
    InputSystem.ICommonActions,
    InputSystem.IPlayerActions
{
    // Common
    public event Action pauseEvent;
    // Player
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> StopMove;

    public Vector2 Move { get; private set; }


    // Common
    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            pauseEvent?.Invoke();
        }
    }
    // Player
    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }
    public void OnStopMove()
    {
        Move = Vector2.zero;
    }
}
