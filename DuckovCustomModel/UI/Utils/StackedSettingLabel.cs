using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Utils
{
    internal sealed class StackedSettingLabel : MonoBehaviour
    {
        private TMP_Text? _label;
        private string? _lastText;
        private float _lastWidth = -1;

        private void LateUpdate()
        {
            _label ??= GetComponent<TMP_Text>();
            var rect = (RectTransform)transform;
            var width = rect.rect.width;
            if (width <= 0 || _label == null ||
                (Mathf.Approximately(width, _lastWidth) && _lastText == _label.text)) return;
            _lastWidth = width;
            _lastText = _label.text;
            var height = Mathf.Max(36, Mathf.Ceil(_label.GetPreferredValues(_label.text, width, Mathf.Infinity).y) + 4);
            rect.offsetMin = new(20, -6 - height);
            rect.offsetMax = new(-20, -6);
            transform.parent.GetComponent<LayoutElement>().preferredHeight = height + 48;
        }
    }
}
