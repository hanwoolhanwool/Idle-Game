using UnityEngine;
using UnityEngine.EventSystems;

public class  SafeArea : UIBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeAtrea = new();

    protected override void Awake()
    {
        base.Awake();
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (Screen.safeArea != lastSafeAtrea)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        if (rectTransform == null) return;
        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        lastSafeAtrea = safeArea;
    }
}