/// <summary>
/// 매 프레임 시간 경과 기반으로 갱신되어야 하는 런타임 시스템의 계약.
/// PlayerRoot가 이 추상에만 의존해 Tick 순회하도록 하여,
/// 새로운 틱 시스템 추가 시 Update()를 수정하지 않도록 한다(OCP).
/// </summary>
public interface ITickable
{
    void Tick(float deltaTime);
}
