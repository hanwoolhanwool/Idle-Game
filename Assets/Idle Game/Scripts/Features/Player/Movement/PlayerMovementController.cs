using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovementController : MonoBehaviour, IPlayerMovementController, IStatDrivenMovement, IMoveInputConsumer
{
    [Header("References")]
    [SerializeField] private MonoBehaviour inputSourceBehaviour;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Header("Move Settings")]
    [SerializeField] private PlayerStat playerStat;

    private IMoveInputSource _inputSource;
    private IReadOnlyStats _stats;
    private Rigidbody2D _rigidbody2D;
    private Vector2 _moveInput;
    private Vector2 _moveDirection;
    
    public Vector2 MoveInput => _moveInput;
    public Vector2 MoveDirection => _moveDirection;
    public Vector2 Velocity => _rigidbody2D.linearVelocity;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();

        // 직렬화된 소스는 조립 루트가 라우터를 주입하기 전까지의 기본값(폴백)이다.
        // 실제 입력 소스는 SetInputSource로 교체될 수 있다(방치↔능동 스왑).
        _inputSource = inputSourceBehaviour as IMoveInputSource;

        if (spriteRenderer == null)
        {
            Debug.LogError($"{nameof(spriteRenderer)} is null!");
            enabled = false;
            return;
        }

        ConfigureRigidBody();
    }

    /// <summary>
    /// 이동 입력 소스를 주입한다. 조립 루트가 입력 라우터를 넘겨
    /// 방치↔능동 모드 전환이 이 컨트롤러에 투명하게 반영되도록 한다.
    /// </summary>
    public void SetInputSource(IMoveInputSource source)
    {
        _inputSource = source;
    }

    /// <summary>
    /// 이동 속도의 단일 출처(Single Source of Truth)를 주입한다.
    /// 조립 루트가 StatMachine을 넘겨, 장비·버프로 변동된 MoveSpeed가
    /// 실제 이동에 반영되도록 한다.
    /// </summary>
    public void BindStats(IReadOnlyStats stats)
    {
        _stats = stats;
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
        _moveInput = _inputSource?.Move ?? Vector2.zero;
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
        // 이동 속도는 런타임 스탯에서 읽는다(SoT). 아직 주입 전이면 정지.
        if (_stats == null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        float moveSpeed = _stats.GetFinal(StatType.MoveSpeed);
        Vector2 targetVelocity = _moveDirection * moveSpeed;
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