using UnityEngine;

namespace DuckovCustomModel.UI.Utils
{
    internal sealed class ModelColumnsLayout : MonoBehaviour
    {
        private RectTransform? _left;
        private RectTransform? _middle;
        private RectTransform? _right;

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        internal void Initialize(RectTransform left, RectTransform middle, RectTransform right)
        {
            _left = left;
            _middle = middle;
            _right = right;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (_left == null || _middle == null || _right == null) return;
            var width = Mathf.Max(0, ((RectTransform)transform).rect.width);
            var gap = Mathf.Min(10, width / 20);
            var available = Mathf.Max(0, width - gap * 2);
            var shrink = Mathf.Clamp01((830 - available) / 240);
            var left = Mathf.Lerp(200, 160, shrink);
            var right = Mathf.Lerp(500, 300, shrink);
            if (available < 590)
            {
                left = available * (160f / 590);
                right = available * (300f / 590);
            }

            Place(_left, 0, left);
            Place(_middle, left + gap, Mathf.Max(0, available - left - right));
            Place(_right, width - right, right);
        }

        private static void Place(RectTransform rect, float x, float width)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new(0, 1);
            rect.offsetMin = new(x, 0);
            rect.offsetMax = new(x + width, 0);
        }
    }
}
