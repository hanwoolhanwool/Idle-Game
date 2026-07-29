using UnityEngine;

/// <summary>
/// 실제 UI가 붙기 전까지 사용하는 로그 기반 HUD 구현.
/// </summary>
public sealed class DebugPlayerHud : IPlayerHud
{
    public void Render(in PlayerHudSnapshot s)
    {
        // 경험치는 최고 레벨에서 분모가 int.MaxValue라 그대로 찍으면 의미 없는 수가 나온다.
        string exp = s.IsMaxLevel
            ? "MAX"
            : $"{NumberFormatter.Format(s.Exp)}/{NumberFormatter.Format(s.RequiredExp)}";

        Debug.Log(
            $"[HUD] Lv.{s.Level} EXP {exp} | " +
            $"GOLD {NumberFormatter.Format(s.Gold)} | " +
            $"HP {s.CurrentHp:0.#}/{s.MaxHp:0.#} | " +
            $"MP {s.CurrentMp:0.#}/{s.MaxMp:0.#} | " +
            $"ATK {s.AttackPower:0.#} | " +
            $"ASPD {s.AttackSpeed:0.##} | " +
            $"MOVE {s.MoveSpeed:0.#} | " +
            $"DPS {s.Dps:0.#}");
    }
}
