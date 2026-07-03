using UnityEngine;

public sealed class PlayerHudBinder : MonoBehaviour
{
    [SerializeField] private bool debugLogOrRefresh = true;

    private PlayerStatComponent _statComponent;

    public void Bind(PlayerStatComponent statComponent)
    {
        Unbind();
        _statComponent = statComponent;
        _statComponent.Stats.OnStatChanged += HandleStatChanged;
        RefreshImmediate(_statComponent);
    }

    public void Unbind()
    {
        if (_statComponent == null)
            return;

        _statComponent.Stats.OnStatChanged -= HandleStatChanged;
        _statComponent = null;
    }

    public void RefreshImmediate(PlayerStatComponent statComponent)
    {
        if (statComponent == null)
            return;
        if (!debugLogOrRefresh)
            return;
        
        Debug.Log(
            $"[HUD] HP {statComponent.CurrentHp}/{statComponent.Stats.GetFinal(StatType.MaxHp)} | " +
            $"MP {statComponent.CurrentMp}/{statComponent.Stats.GetFinal(StatType.MaxMp)} | " +
            $"ATK {statComponent.Stats.GetFinal(StatType.AttackPower)} | " +
            $"ASPD {statComponent.Stats.GetFinal(StatType.AttackSpeed)} | " +
            $"MOVE {statComponent.Stats.GetFinal(StatType.MoveSpeed)} | " +
            $"DPS {statComponent.ComputeDps()}");
    }

    private void HandleStatChanged(StatType statType, float value)
    {
        RefreshImmediate(_statComponent);
    }

    private void OnDestroy()
    {
        Unbind();
    }
}