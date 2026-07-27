/// <summary>
/// 저장 대상 시스템의 계약. 각 도메인이 <b>자기 섹션만</b> 채우고 읽는다(SRP).
/// 새 시스템(퀘스트·업적)이 저장 대상이 되어도 <see cref="SaveService"/>는 수정되지 않고
/// 구현을 등록만 하면 된다(OCP, 정본 §7 "조각 자치").
/// </summary>
public interface ISaveable
{
    /// <summary>현재 런타임 상태를 세이브 DTO에 기록한다(저장 시).</summary>
    void CaptureState(PlayerSaveData data);

    /// <summary>세이브 DTO를 읽어 런타임 상태를 복원한다(로드 시).</summary>
    void RestoreState(PlayerSaveData data);
}
