using UnityEngine;

namespace DuckovCustomModel.UI.Utils
{
    internal sealed class SettingsColumnsLayout : MonoBehaviour
    {
        private Vector2 _lastSize = new(-1, -1);
        private RectTransform? _settings;
        private RectTransform? _updates;

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        internal void Initialize(RectTransform settings, RectTransform updates)
        {
            _settings = settings;
            _updates = updates;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (_settings == null || _updates == null) return;
            var rect = ((RectTransform)transform).rect;
            if (_lastSize == rect.size) return;
            _lastSize = rect.size;
            if (rect.width >= 760)
            {
                var updatesWidth = (rect.width - 36) * .5f;
                Place(_settings, Vector2.zero, Vector2.one, new(12, 12), new(-updatesWidth - 24, -12));
                Place(_updates, new(1, 0), Vector2.one, new(-updatesWidth - 12, 12), new(-12, -12));
            }
            else
            {
                var updatesHeight = Mathf.Min(Mathf.Clamp(rect.height * .48f, 220, 340),
                    Mathf.Max(0, rect.height - 144));
                Place(_settings, Vector2.zero, Vector2.one, new(12, updatesHeight + 24), new(-12, -12));
                Place(_updates, Vector2.zero, new(1, 0), new(12, 12), new(-12, updatesHeight + 12));
            }
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = insetMin;
            rect.offsetMax = insetMax;
        }
    }
}
