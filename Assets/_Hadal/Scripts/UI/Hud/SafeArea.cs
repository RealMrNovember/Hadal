using UnityEngine;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// Keeps a full-screen RectTransform inside <see cref="Screen.safeArea"/> (notch, rounded corners,
    /// gesture bar). Re-applies only when the safe area or the resolution changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied;
        private Vector2Int _screen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _screen.x || Screen.height != _screen.y)
                Apply();
        }

        private void Apply()
        {
            _applied = Screen.safeArea;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            _rect.anchorMin = new Vector2(_applied.xMin / Screen.width, _applied.yMin / Screen.height);
            _rect.anchorMax = new Vector2(_applied.xMax / Screen.width, _applied.yMax / Screen.height);
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }
    }
}
