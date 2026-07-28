using UnityEngine;

/// <summary>
/// 앱 수준의 생명주기 훅을 담당한다. 저장 <b>정책의 시점</b>(앱이 백그라운드로 갈 때·종료할 때)만
/// 알고, 무엇을 어떻게 저장하는지는 모른다 — 그건 <c>SaveService</c>의 책임이다(SRP).
/// 주기 저장은 <c>PlayerRoot</c>의 틱 순회가 담당하므로 여기에는 <c>Update</c>가 없다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    [Tooltip("저장을 요청할 플레이어 조립 루트. 비어 있으면 씬에서 찾는다.")]
    [SerializeField] private PlayerRoot playerRoot;

    private void Awake()
    {
        if (playerRoot == null)
            playerRoot = FindFirstObjectByType<PlayerRoot>();

        if (playerRoot == null)
            Debug.LogWarning("[GameManager] PlayerRoot를 찾지 못해 일시정지·종료 시 저장이 동작하지 않습니다.", this);
    }

    /// <summary>
    /// 모바일에서 <b>실질적인 마지막 저장 기회</b>다. OS가 백그라운드 앱을 임의로 죽이기 때문에
    /// <see cref="OnApplicationQuit"/>은 호출이 보장되지 않는다(정본 §6.3).
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            playerRoot?.SaveNow();
    }

    /// <summary>에디터·PC에서의 종료 경로. 모바일에서는 유일 의존 금지.</summary>
    private void OnApplicationQuit()
    {
        playerRoot?.SaveNow();
    }
}
