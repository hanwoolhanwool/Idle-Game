/// <summary>
/// 플레이어 제어 주체(하이브리드 방치+능동 장르의 두 모드).
/// </summary>
public enum PlayerControlMode
{
    /// <summary>능동: 사람이 조이스틱으로 직접 조작.</summary>
    Active,

    /// <summary>방치: 자동 전투 AI가 이동/타겟을 결정.</summary>
    Idle,
}
