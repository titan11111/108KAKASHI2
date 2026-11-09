using UnityEngine;

namespace Kakashi
{
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        int width, height;
        void OnEnable() { Apply(); }
        void Update()
        {
            if (last != Screen.safeArea || width != Screen.width || height != Screen.height) Apply();
        }
        void Apply()
        {
            width = Screen.width; height = Screen.height; last = Screen.safeArea;
            if (width <= 0 || height <= 0) return;
            var r = (RectTransform)transform;
            r.anchorMin = new Vector2(last.xMin / width, last.yMin / height);
            r.anchorMax = new Vector2(last.xMax / width, last.yMax / height);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
