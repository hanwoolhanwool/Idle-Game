public sealed class PlayerBaseStatResolver : IPlayerBaseStatResolver
{
    public PlayerBaseStatSet Resolve(PlayerProgressionState progressionState, PlayerProgressionConfig config)
    {
        // 샘플은 config 직접 사용.
        // 실무에서는 레벨 테이블, 클래스, 승급, 연구 데이터를 합산해서 만든다.
        return new PlayerBaseStatSet
        {
            MaxHp = config.StartMaxHp,
            AttackPower = config.StartAttackPower,
            AttackSpeed = config.StartAttackSpeed,
            MoveSpeed = config.StartMoveSpeed,
            Defense = config.StartDefense,
            MaxMana = config.StartMaxMana,
            HpRegen = config.StartHpRegen,
            MpRegen = config.StartManaRegen,
        };
    }
}