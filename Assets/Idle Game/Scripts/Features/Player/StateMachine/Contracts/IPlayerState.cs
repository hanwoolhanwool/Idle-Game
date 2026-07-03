public interface IPlayerState
{
    PlayerStateID StateID { get; }
    
    void Enter();
    void Exit();
    
    void Tick(float deltaTime);
    void FixedTick(float fixedDeltaTime);
}