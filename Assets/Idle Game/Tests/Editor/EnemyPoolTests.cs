using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 오브젝트 풀의 재사용 보장 검증. (M0 계획서 §10 케이스 7)
/// <para>
/// 풀이 재사용에 실패하면 방치 중 Instantiate/Destroy가 무한 반복되어
/// GC 스파이크로 프레임이 끊긴다 — 방치형에서 가장 치명적인 성능 결함이다.
/// </para>
/// </summary>
public sealed class EnemyPoolTests
{
    private GameObject _prefabObject;
    private GameObject _parentObject;
    private EnemyUnit _prefab;
    private Transform _parent;

    [SetUp]
    public void SetUp()
    {
        _prefabObject = new GameObject("EnemyPrefab");
        _prefab = _prefabObject.AddComponent<EnemyUnit>();

        // 풀이 만든 인스턴스를 한 번에 정리하기 위한 컨테이너.
        _parentObject = new GameObject("PoolParent");
        _parent = _parentObject.transform;
    }

    [TearDown]
    public void TearDown()
    {
        // 자식(풀 인스턴스)이 부모와 함께 파괴되어 에디터 씬에 잔류하지 않는다.
        Object.DestroyImmediate(_parentObject);
        Object.DestroyImmediate(_prefabObject);
    }

    [Test]
    public void Rent_Return_Rent가_같은_인스턴스를_재사용한다()
    {
        var pool = new EnemyPool(_prefab, _parent, prewarm: 1);

        EnemyUnit first = pool.Rent();
        pool.Return(first);
        EnemyUnit second = pool.Rent();

        Assert.AreSame(first, second, "풀이 인스턴스를 재사용하지 않고 매번 새로 만들고 있습니다.");
    }

    [Test]
    public void Rent_재고가_없으면_새로_생성한다()
    {
        var pool = new EnemyPool(_prefab, _parent, prewarm: 0);

        EnemyUnit unit = pool.Rent();

        // 재고 부족이 스폰 실패로 이어지면 안 된다(적이 끊기면 방치형이 성립하지 않는다).
        Assert.IsNotNull(unit);
    }

    [Test]
    public void Rent_동시에_빌린_인스턴스는_서로_다르다()
    {
        var pool = new EnemyPool(_prefab, _parent, prewarm: 2);

        EnemyUnit a = pool.Rent();
        EnemyUnit b = pool.Rent();

        // 같은 인스턴스를 두 번 내주면 한 마리를 두 마리처럼 취급하게 된다.
        Assert.AreNotSame(a, b);
    }

    [Test]
    public void 빌려온_인스턴스는_비활성_상태다()
    {
        var pool = new EnemyPool(_prefab, _parent, prewarm: 1);

        EnemyUnit unit = pool.Rent();

        // 활성화 시점은 스포너가 배치·초기화를 끝낸 뒤여야 한다.
        // 풀이 켠 채로 내주면 스탯이 주입되기 전 한 프레임 동안 기본값 적으로 존재한다.
        Assert.IsFalse(unit.gameObject.activeSelf);
    }

    [Test]
    public void Return_null은_무시한다()
    {
        var pool = new EnemyPool(_prefab, _parent, prewarm: 0);

        // 파괴된 인스턴스가 반납되는 경로(씬 전환 등)에서 앱이 죽지 않아야 한다.
        Assert.DoesNotThrow(() => pool.Return(null));
    }

    [Test]
    public void Prewarm_수만큼_미리_생성해_둔다()
    {
        var pool = new EnemyPool(_prefab, _parent, prewarm: 3);

        // 부모 아래 자식 수로 확인한다. 런타임에 Instantiate를 반복하지 않는 것이 풀의 목적이다.
        Assert.AreEqual(3, _parent.childCount);
    }
}
