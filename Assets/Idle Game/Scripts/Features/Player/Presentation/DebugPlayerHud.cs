using UnityEngine;

/// <summary>
/// 실제 UI가 붙기 전까지 사용하는 로그 기반 HUD 구현.
/// </summary>
public sealed class DebugPlayerHud : IPlayerHud
{
    public void Render(in PlayerHudSnapshot s)
    {
        Debug.Log(
            $"[HUD] HP {s.CurrentHp:0.#}/{s.MaxHp:0.#} | " +
            $"MP {s.CurrentMp:0.#}/{s.MaxMp:0.#} | " +
            $"ATK {s.AttackPower:0.#} | " +
            $"ASPD {s.AttackSpeed:0.##} | " +
            $"MOVE {s.MoveSpeed:0.#} | " +
            $"DPS {s.Dps:0.#}");
    }
}
