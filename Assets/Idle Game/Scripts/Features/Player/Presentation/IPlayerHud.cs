/// <summary>
/// 플레이어 HUD 표시 추상. 로그 구현(<see cref="DebugPlayerHud"/>)과 실제 UI 구현을
/// 교체 가능하게 해, 프레젠테이션 결합도를 낮춘다. (DIP / OCP)
/// </summary>
public interface IPlayerHud
{
    void Render(in PlayerHudSnapshot snapshot);
}
