using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 로컬 파일(JSON) 기반 세이브 저장소. <b>원자적 쓰기</b>로 쓰는 도중 앱이 죽어도
/// 기존 세이브가 살아남게 한다(정본 §6.5) — 그냥 덮어쓰면 세이브 전체가 유실된다.
/// <para>
/// 직렬화는 <c>JsonUtility</c>를 쓴다. M0 범위(Progression·Wallet)에는 딕셔너리·다형성이
/// 없어 충분하며, 교체가 필요해지면(인벤토리 단계) <b>이 클래스만</b> 고치면 된다.
/// </para>
/// </summary>
public sealed class FileSaveRepository : ISaveRepository
{
    private readonly string _savePath;
    private readonly string _tempPath;
    private readonly string _backupPath;

    /// <param name="directory">저장 폴더. 보통 <c>Application.persistentDataPath</c>.</param>
    public FileSaveRepository(string directory, string fileName = "save.json")
    {
        _savePath = Path.Combine(directory, fileName);
        _tempPath = _savePath + ".tmp";
        _backupPath = _savePath + ".bak";
    }

    /// <summary>본 파일 → 실패 시 백업 순으로 시도한다. 둘 다 실패해도 예외 대신 false(신규 게임으로 폴백).</summary>
    public bool TryLoad(out PlayerSaveData data)
    {
        if (TryReadFrom(_savePath, out data))
            return true;

        if (TryReadFrom(_backupPath, out data))
        {
            Debug.LogWarning("[Save] 세이브가 손상되어 백업(.bak)에서 복구했습니다.");
            return true;
        }

        data = null;
        return false;
    }

    public void Save(PlayerSaveData data)
    {
        if (data == null)
            return;

        try
        {
            // 1) 임시 파일에 먼저 기록한다. 여기서 죽어도 기존 save.json은 무손상이다.
            File.WriteAllText(_tempPath, JsonUtility.ToJson(data, prettyPrint: true));

            // 2) 기존 본을 백업으로 남긴다(3)의 짧은 공백 구간을 이 파일이 메운다).
            if (File.Exists(_savePath))
            {
                File.Copy(_savePath, _backupPath, overwrite: true);
                File.Delete(_savePath);
            }

            // 3) 임시 → 본으로 이름 변경. 내용 쓰기가 아니라 이름만 바뀌므로 중간 상태가 없다.
            File.Move(_tempPath, _savePath);
        }
        catch (Exception e)
        {
            // 저장 실패가 게임을 죽이지는 않는다. 다음 주기 저장에서 다시 시도된다.
            Debug.LogError($"[Save] 저장에 실패했습니다: {e.Message}");
        }
    }

    public void Delete()
    {
        TryDelete(_savePath);
        TryDelete(_tempPath);
        TryDelete(_backupPath);
    }

    private static bool TryReadFrom(string path, out PlayerSaveData data)
    {
        data = null;

        try
        {
            if (!File.Exists(path))
                return false;

            data = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(path));

            // JsonUtility는 깨진 JSON에서 null을 돌려주기도 하고 예외를 던지기도 한다. 둘 다 실패로 취급한다.
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] '{path}' 읽기 실패: {e.Message}");
            data = null;
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] '{path}' 삭제 실패: {e.Message}");
        }
    }
}
