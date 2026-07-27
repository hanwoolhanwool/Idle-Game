/// <summary>
/// 세이브 데이터의 저장 매체를 감추는 경계 계약.
/// 어떤 컨트롤러도 파일 경로·JSON·persistentDataPath를 알지 못한다 —
/// 나중에 서버 저장(<c>ServerSaveRepository</c>)으로 갈아탈 때 호출부가 한 줄도 안 바뀐다(LSP·DIP).
/// </summary>
public interface ISaveRepository
{
    /// <summary>저장본을 읽는다. 없거나 복구 불가하면 <c>false</c>(예외를 던지지 않는다).</summary>
    bool TryLoad(out PlayerSaveData data);

    /// <summary>저장본을 기록한다. 원자성 보장은 구현의 책임이다.</summary>
    void Save(PlayerSaveData data);

    /// <summary>저장본을 삭제한다(초기화·테스트용).</summary>
    void Delete();
}
