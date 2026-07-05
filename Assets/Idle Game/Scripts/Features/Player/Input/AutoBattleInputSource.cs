using UnityEngine;

public sealed class AutoBattleInputSource : MonoBehaviour, IMoveInputSource
{
    [SerializeField] private float attackRange = 2f;
    
    private ITargetProvider _targetProvider;
    
    public Vector2 Move { get; private set; }
    public Transform CurrentTarget { get; private set; }
    public bool InAttackRange { get; private set; }

    public void Initialize(ITargetProvider targetProvider)
    {
        _targetProvider = targetProvider;
    }

    private void Update()
    {
        if (_targetProvider == null)
        {
            Move = Vector2.zero;
            return;
        }

        Vector2 self = transform.position;
        Transform target = _targetProvider.GetNearestTarget(self);
        CurrentTarget = target;
        if (target == null)
        {
            Move = Vector2.zero;
            InAttackRange = false;
            return;
        }
        
        Vector2 toTarget = (Vector2)target.position - self;
        if (toTarget.sqrMagnitude <= attackRange * attackRange)
        {
            Move = Vector2.zero;
            InAttackRange = true;
        }
        else
        {
            Move = toTarget.normalized;
            InAttackRange = false;
        }
    }
    
}