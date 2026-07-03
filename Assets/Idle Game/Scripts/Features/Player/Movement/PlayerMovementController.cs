using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovementController : MonoBehaviour, IPlayerMovementController
{
    [Header("References")]
    [SerializeField] private JoystickInputReader inputReader;
    [SerializeField] private SpriteRenderer  spriteRenderer;
    
    [Header("Move Settings")]
    [SerializeField] private PlayerStat playerStat;
    
    private Rigidbody2D _rigidbody2D;
    private Vector2 _moveInput;
    private Vector2 _moveDirection;
    
    public Vector2 MoveInput => _moveInput;
    public Vector2 MoveDirection => _moveDirection;
    public Vector2 Velocity => _rigidbody2D.linearVelocity;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();

        if (inputReader == null)
        {
            Debug.LogError($"{nameof(inputReader)} is null!");
            enabled = false;
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError($"{nameof(spriteRenderer)} is null!");
            enabled = false;
            return; 
        }

        ConfigureRigidBody();
    }

    private void Update()
    {
        ReadInput();
        UpdateVisual();
    }

    private void FixedUpdate()
    {
        Move();
    }
    
    private void ConfigureRigidBody()
    {
        _rigidbody2D.gravityScale = 0f;
        _rigidbody2D.freezeRotation = true;
        _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void ReadInput()
    {
        _moveInput = inputReader.Move;
        if (_moveInput.sqrMagnitude < playerStat.InputDeadZone * playerStat.InputDeadZone)
        {
            _moveInput = Vector2.zero;
            _moveDirection = Vector2.zero;
            return;
        }
        _moveDirection = _moveInput.normalized;
    }

    private void Move()
    {
        Vector2 targetVelocity = _moveDirection * playerStat.MoveSpeed;
        _rigidbody2D.linearVelocity = targetVelocity;
    }

    private void UpdateVisual()
    {
        if (!playerStat.useSpriteFlip)
            return;
        if(_moveDirection.x > 0.01f)
            spriteRenderer.flipX = true;
        else if(_moveDirection.x < -0.01f)
            spriteRenderer.flipX = false;
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled)
        {
            _moveInput = Vector2.zero;
            _moveDirection = Vector2.zero;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        this.enabled = enabled;
    }

    private void OnDisable()
    {
        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
        }
    }
}