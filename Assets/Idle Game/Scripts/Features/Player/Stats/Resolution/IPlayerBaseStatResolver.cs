/// <summary>
/// 성장 상태(레벨·전직 차수)로부터 베이스 스탯을 산출하는 계약.
///
/// <para>
/// 산출 근거(레벨 테이블·전직 보정 등)는 전적으로 구현이 소유하며 호출부는 알지 못한다(DIP).
/// 인자에서 <c>PlayerProgressionConfig</c>가 빠진 이유: config는 "시작 상태"를 담는 데이터이고,
/// "레벨에 따른 성장 규칙"은 <see cref="PlayerLevelTable"/>의 책임이다(SRP).
/// 리졸버가 두 출처를 동시에 참조하면 스탯의 진실 공급원이 둘로 갈라진다.
/// </para>
/// </summary>
public interface IPlayerBaseStatResolver
{
    /// <summary>현재 성장 상태에 해당하는 베이스 스탯 집합을 만든다.</summary>
    PlayerBaseStatSet Resolve(PlayerProgressionState progressionState);
}
