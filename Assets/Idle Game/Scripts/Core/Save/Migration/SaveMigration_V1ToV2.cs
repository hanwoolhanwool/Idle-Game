/// <summary>
/// 세이브 스키마 v1 → v2 변환. v2에서 <see cref="WorldSaveSection"/>(스테이지 진척·시간 기준선)이
/// 추가됐다.
/// <para>
/// <b>이 변환은 실제로 아무 데이터도 손대지 않는다.</b> <c>JsonUtility</c>는 세이브에 없는 필드를
/// 클래스 초기화값으로 채우므로, v1 세이브를 v2 모델로 읽으면 World 섹션이 이미 기본값
/// (StageId 빈 문자열 · 처치 0 · 기준 시각 0)으로 존재한다. 그 기본값이 정확히
/// "첫 스테이지에서 시작하고 오프라인 보상은 없음"을 뜻해 손볼 것이 없다.
/// </para>
/// <para>
/// 그럼에도 <b>등록하는 이유</b>: 구현이 없으면 v1 세이브를 가진 기존 유저의 로드마다
/// <c>SaveService</c>가 "v1 → v2 마이그레이션이 없습니다" 경고를 찍는다. 그 경고는 변환 누락이라는
/// 진짜 사고를 알리기 위한 신호인데, 정상 상황에서 매번 울리면 신호로서의 값을 잃는다.
/// 빈 변환기 하나가 그 신호를 지켜 준다.
/// </para>
/// </summary>
public sealed class SaveMigration_V1ToV2 : ISaveMigration
{
    public int FromVersion => 1;

    public void Migrate(PlayerSaveData data)
    {
        // 필드 추가만 있었던 버전이라 변환할 값이 없다.
        // 섹션 자체가 null인 비정상 세이브만 방어한다(수동 편집·부분 손상).
        data.World ??= new WorldSaveSection();
    }
}
