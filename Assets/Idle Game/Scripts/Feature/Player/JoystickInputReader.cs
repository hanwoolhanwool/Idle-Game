using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickInputReader : MonoBehaviour, IJoystickInputReader
{
    [Header("Input Reader References")]
    [SerializeField] private InputActionReference moveAction;
    public Vector2 Move { get; private set; }

    private InputAction _moveInputAction;
    private void Awake()
    {
        if (moveAction == null)
        {
            Debug.Log($"{nameof(JoystickInputReader)}: Input Action Reference is not assigned");
            enabled = false;
        }
        _moveInputAction = moveAction.action;

        if(_moveInputAction == null)
        {
            Debug.Log($"{nameof(JoystickInputReader)}: Input Action is not assigned in the Input Action Reference");
            enabled = false;
        }
    }
    private void OnEnable()
    {
        if (_moveInputAction == null)
        {
            Debug.Log($"{nameof(JoystickInputReader)}: Input Action Reference is not assigned");
            return;
        }
        
        _moveInputAction.Enable();
        _moveInputAction.performed += OnMove;
        _moveInputAction.canceled += OnMoveCanceled;
    }
    private void OnDisable()
    {
        _moveInputAction.performed -= OnMove;
        _moveInputAction.canceled -= OnMoveCanceled;
        _moveInputAction.Disable();

        Move = Vector2.zero;
    }
    private void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        Move = Vector2.zero;
    }
}