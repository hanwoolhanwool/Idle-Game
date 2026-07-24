using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 인스턴스를 재사용하는 오브젝트 풀. 런타임에 Instantiate/Destroy를 반복하지 않고,
/// 죽은 적을 비활성 상태로 보관했다가 다시 꺼내 쓴다(GC 스파이크·힙 할당 방지).
/// 스포너가 소유하는 순수 C# 도구이며, "몇 마리를 언제 유지할지"는 모른다(SRP).
/// </summary>
public sealed class EnemyPool
{
    private readonly EnemyUnit _prefab;
    private readonly Transform _parent;
    private readonly Stack<EnemyUnit> _inactive = new();

    public EnemyPool(EnemyUnit prefab, Transform parent, int prewarm)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < prewarm; i++)
            _inactive.Push(CreateInactive());
    }

    /// <summary>재사용할 적을 비활성 상태로 반환한다. 재고가 없으면 새로 생성한다.</summary>
    public EnemyUnit Rent()
    {
        return _inactive.Count > 0 ? _inactive.Pop() : CreateInactive();
    }

    /// <summary>적을 풀에 반납한다. 비활성화가 곧 EnemyRegistry 해제다(EnemyUnit.OnDisable).</summary>
    public void Return(EnemyUnit unit)
    {
        if (unit == null)
            return;

        unit.gameObject.SetActive(false);
        _inactive.Push(unit);
    }

    private EnemyUnit CreateInactive()
    {
        EnemyUnit unit = Object.Instantiate(_prefab, _parent);
        unit.gameObject.SetActive(false);
        return unit;
    }
}
