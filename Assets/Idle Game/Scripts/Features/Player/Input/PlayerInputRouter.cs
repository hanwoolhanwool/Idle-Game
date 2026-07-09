using UnityEngine;

/// <summary>
/// 여러 입력 소스(능동 조이스틱 / 방치 자동전투)를 감싸, 현재 활성 소스로 위임하는 라우터.
/// 자신도 <see cref="IMoveInputSource"/>라서 소비처(이동 컨트롤러)는 이 라우터 하나만 바라본다.
/// 모드 소유·전환은 조립 루트(PlayerRoot)가 <see cref="SetMode"/>로 수행한다. (하이브리드 장르의 핵심 스왑 지점)
/// </summary>
public sealed class PlayerInputRouter : IMoveInputSource
{
    private readonly IMoveInputSource _activeSource;
    private readonly IMoveInputSource _idleSource;

    private PlayerControlMode _mode;
    private IMoveInputSource _current;

    public PlayerInputRouter(
        IMoveInputSource activeSource,
        IMoveInputSource idleSource,
        PlayerControlMode initialMode = PlayerControlMode.Active)
    {
        _activeSource = activeSource;
        _idleSource = idleSource;
        SetMode(initialMode);
    }

    public PlayerControlMode Mode => _mode;

    /// <summary>현재 활성 소스의 이동 입력. 소스 미연결 시 정지(0).</summary>
    public Vector2 Move => _current?.Move ?? Vector2.zero;

    /// <summary>활성 입력 소스를 런타임에 교체한다.</summary>
    public void SetMode(PlayerControlMode mode)
    {
        _mode = mode;
        _current = mode == PlayerControlMode.Active ? _activeSource : _idleSource;
    }

    /// <summary>현재 모드의 반대 모드로 토글한다.</summary>
    public void ToggleMode()
    {
        SetMode(_mode == PlayerControlMode.Active ? PlayerControlMode.Idle : PlayerControlMode.Active);
    }
}
