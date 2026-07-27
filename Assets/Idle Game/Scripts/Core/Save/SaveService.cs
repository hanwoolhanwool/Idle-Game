using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저장 조율자. 로드/신규 분기를 <b>한 곳</b>에 모으고, 등록된 <see cref="ISaveable"/> 조각들을
/// 모아 하나의 <see cref="PlayerSaveData"/>로 조립한다. 저장 매체는 <see cref="ISaveRepository"/>
/// 뒤에 숨어 있어 이 클래스는 파일도 JSON도 모른다(DIP).
/// <para>
/// MonoBehaviour가 아니라 <see cref="ITickable"/>이다. PlayerRoot의 기존 틱 순회에 얹히므로
/// 주기 저장을 추가하면서 <c>Update()</c>를 수정하지 않는다(OCP, 정본 §9).
/// </para>
/// </summary>
public sealed class SaveService : ITickable
{
    /// <summary>현재 앱이 이해하는 스키마 버전.</summary>
    public const int CurrentVersion = 1;

    private readonly ISaveRepository _repository;
    private readonly List<ISaveable> _saveables = new();
    private readonly IReadOnlyList<ISaveMigration> _migrations;
    private readonly float _autoSaveInterval;

    private float _elapsed;

    /// <summary>
    /// 앱보다 높은 버전의 세이브를 만난 경우 <c>true</c>. 이때는 <b>저장을 거부</b>한다 —
    /// 구버전 앱이 신버전 세이브를 덮어쓰면 유저 데이터가 파괴되기 때문이다(정본 §6.8).
    /// </summary>
    public bool IsSaveBlocked { get; private set; }

    /// <param name="autoSaveInterval">주기 저장 간격(초). JSON 직렬화는 GC를 유발하므로 30초 이상 권장(정본 §9).</param>
    /// <param name="migrations">M0는 등록 0개(pass-through). 계약과 호출 지점만 심어둔다.</param>
    public SaveService(
        ISaveRepository repository,
        float autoSaveInterval = 60f,
        IReadOnlyList<ISaveMigration> migrations = null)
    {
        _repository = repository;
        _autoSaveInterval = autoSaveInterval;
        _migrations = migrations ?? new List<ISaveMigration>();
    }

    /// <summary>저장 대상을 등록한다. 등록 순서가 곧 Capture/Restore 순서다.</summary>
    public void Register(ISaveable saveable)
    {
        if (saveable != null && !_saveables.Contains(saveable))
            _saveables.Add(saveable);
    }

    /// <summary>
    /// 세이브를 읽어 복원한다. 파일이 없으면 신규 게임으로 시작한다(예외 아님).
    /// 신규일 때는 각 도메인이 이미 자기 기본값(config)을 갖고 있으므로 아무것도 덮어쓰지 않는다.
    /// </summary>
    public void LoadAndRestore()
    {
        if (!_repository.TryLoad(out PlayerSaveData data))
        {
            Debug.Log("[Save] 저장본이 없어 신규 게임으로 시작합니다.");
            return;
        }

        if (data.Version > CurrentVersion)
        {
            IsSaveBlocked = true;
            Debug.LogError(
                $"[Save] 세이브 버전({data.Version})이 앱({CurrentVersion})보다 높습니다. " +
                "데이터 보호를 위해 복원과 저장을 모두 중단합니다.");
            return;
        }

        Migrate(data);

        for (int i = 0; i < _saveables.Count; i++)
            _saveables[i].RestoreState(data);
    }

    /// <summary>등록된 조각들의 상태를 모아 즉시 저장한다.</summary>
    public void SaveNow()
    {
        if (IsSaveBlocked)
            return;

        var data = new PlayerSaveData { Version = CurrentVersion };
        for (int i = 0; i < _saveables.Count; i++)
            _saveables[i].CaptureState(data);

        _repository.Save(data);

        // 주기 저장 타이머를 되감는다. 수동 저장(앱 일시정지 등)이 일어나면
        // 그 시점부터 다시 간격을 세는 것이 맞다.
        _elapsed = 0f;
    }

    /// <summary>주기 저장. 매 프레임 저장하면 모바일 I/O가 감당하지 못한다.</summary>
    public void Tick(float deltaTime)
    {
        if (IsSaveBlocked)
            return;

        _elapsed += deltaTime;
        if (_elapsed < _autoSaveInterval)
            return;

        SaveNow();
    }

    /// <summary>
    /// 버전이 낮은 세이브를 단계적으로 최신화한다. M0는 등록된 마이그레이션이 없어 그대로 통과한다.
    /// <para>
    /// 인접 버전 변환기만 만들고 이를 <b>연쇄 적용</b>한다. v1→v3 같은 직행 변환기를 두면
    /// 버전 N개에 변환기가 N²/2개 필요해지지만, 단계적 방식은 N-1개로 끝난다(OCP).
    /// </para>
    /// </summary>
    private void Migrate(PlayerSaveData data)
    {
        while (data.Version < CurrentVersion)
        {
            ISaveMigration step = FindMigration(data.Version);
            if (step == null)
            {
                // 변환 경로가 없으면 버전만 올려 무한 루프를 막는다(누락된 마이그레이션 방어).
                Debug.LogWarning($"[Save] v{data.Version} → v{data.Version + 1} 마이그레이션이 없습니다. 건너뜁니다.");
                data.Version++;
                continue;
            }

            step.Migrate(data);
            data.Version++;
        }
    }

    private ISaveMigration FindMigration(int fromVersion)
    {
        for (int i = 0; i < _migrations.Count; i++)
        {
            if (_migrations[i].FromVersion == fromVersion)
                return _migrations[i];
        }

        return null;
    }
}
