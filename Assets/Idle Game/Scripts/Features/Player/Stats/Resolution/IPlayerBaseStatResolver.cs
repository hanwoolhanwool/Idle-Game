public interface IPlayerBaseStatResolver
{ 
    PlayerBaseStatSet Resolve(PlayerProgressionState progressionState, PlayerProgressionConfig config);
}