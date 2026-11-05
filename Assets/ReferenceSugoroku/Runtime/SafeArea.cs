using UnityEngine;
namespace ReferenceSugoroku
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect last;
        int width, height;
        void OnEnable() { Apply(); }
        void Update() { if (last != Screen.safeArea || width != Screen.width || height != Screen.height) Apply(); }
        void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            last = Screen.safeArea; width = Screen.width; height = Screen.height;
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(last.xMin / width, last.yMin / height);
            rect.anchorMax = new Vector2(last.xMax / width, last.yMax / height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
