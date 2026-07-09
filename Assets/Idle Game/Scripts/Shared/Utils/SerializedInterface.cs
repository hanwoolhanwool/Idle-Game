using UnityEngine;

/// <summary>
/// 인스펙터에서 <see cref="MonoBehaviour"/>/<see cref="Object"/> 슬롯으로 받은 참조를
/// 인터페이스 <typeparamref name="T"/>로 안전하게 해석하는 헬퍼.
/// 오배선(잘못 드래그 등) 시 컨텍스트·필드명을 담은 에러를 한 곳에서 로깅해
/// 파일마다 복붙되던 검증 코드를 제거한다. (DRY)
/// </summary>
public static class SerializedInterface
{
    public static bool TryResolve<T>(Object candidate, string fieldName, Object context, out T result)
        where T : class
    {
        result = candidate as T;
        if (result != null)
            return true;

        string actual = candidate == null ? "null" : candidate.GetType().Name;
        Debug.LogError(
            $"{(context != null ? context.name : "Unknown")}: '{fieldName}'가 {typeof(T).Name}를 구현해야 합니다. (현재: {actual})",
            context);
        return false;
    }
}
