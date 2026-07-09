#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 에디터 전용 디버그 명령 모음. 인스펙터 ContextMenu로 PlayerRoot의
/// 디버그 훅을 호출한다. 조립 루트(PlayerRoot)에서 디버그 관심사를 분리하고,
/// 파일 전체가 #if UNITY_EDITOR로 감싸여 빌드에는 포함되지 않는다.
/// </summary>
[RequireComponent(typeof(PlayerRoot))]
public sealed class PlayerDebugCommands : MonoBehaviour
{
    [SerializeField] private PlayerRoot playerRoot;
    [SerializeField] private float testDamage = 25f;
    [SerializeField] private int testExp = 120;

    private void Reset()
    {
        playerRoot = GetComponent<PlayerRoot>();
    }

    private PlayerRoot Root => playerRoot != null ? playerRoot : GetComponent<PlayerRoot>();

    [ContextMenu("Apply Test Damage")]
    private void ApplyTestDamage() => Root.DebugApplyDamage(testDamage);

    [ContextMenu("Apply First Start Buff")]
    private void ApplyFirstStartBuff() => Root.DebugApplyFirstStartBuff();

    [ContextMenu("Gain Test Exp")]
    private void GainTestExp() => Root.DebugGainExp(testExp);

    [ContextMenu("Toggle Control Mode (Active/Idle)")]
    private void ToggleControlMode() => Root.DebugToggleControlMode();
}
#endif
