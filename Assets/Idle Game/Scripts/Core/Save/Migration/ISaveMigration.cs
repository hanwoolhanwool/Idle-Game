/// <summary>
/// 세이브 스키마 마이그레이션 계약. 각 구현은 <b>한 단계(N → N+1)만</b> 책임진다.
/// 여러 버전을 건너뛴 세이브도 단계를 순차 적용해 최신화되므로, 새 버전이 생기면
/// 구현을 <b>하나 추가</b>할 뿐 기존 것은 수정되지 않는다(OCP, 정본 §6.6).
/// <para>
/// M0는 <b>계약만 확정</b>하고 등록 0개로 통과(pass-through)시킨다. 출시 전이라 변환할
/// 구버전 세이브가 세상에 없기 때문이다(YAGNI). 다만 호출 지점을 지금 심어두지 않으면
/// 나중에 <see cref="SaveService"/>의 로드 경로를 헤집어야 한다.
/// </para>
/// </summary>
public interface ISaveMigration
{
    /// <summary>이 마이그레이션이 소비하는 버전(이 값과 같은 세이브에만 적용된다).</summary>
    int FromVersion { get; }

    /// <summary>데이터를 <see cref="FromVersion"/> → +1 형태로 변환한다. Version 증가는 SaveService의 책임.</summary>
    void Migrate(PlayerSaveData data);
}
