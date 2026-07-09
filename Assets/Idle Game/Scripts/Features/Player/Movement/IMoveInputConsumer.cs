/// <summary>
/// 이동 입력 소스를 런타임에 주입받는 계약.
/// 조립 루트가 입력 라우터(<see cref="PlayerInputRouter"/>)를 넘겨,
/// 이동 컨트롤러가 어떤 소스에서 입력이 오는지 알 필요 없이 동작하게 한다. (DIP)
/// </summary>
public interface IMoveInputConsumer
{
    void SetInputSource(IMoveInputSource source);
}
