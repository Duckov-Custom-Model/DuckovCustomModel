using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Utils
{
    internal sealed class ButtonOffsetRowLayout : LayoutGroup
    {
        private RectTransform[] _items = [];
        private float _lineHeight = 36;
        private bool _stacked;
        private float[] _widths = [];

        internal void Initialize(params GameObject[] items)
        {
            _items = new RectTransform[items.Length];
            _widths = new float[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                _items[i] = (RectTransform)items[i].transform;
                var text = items[i].GetComponent<TMP_Text>();
                if (text == null) continue;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }

            var layout = GetComponent<LayoutElement>();
            layout.preferredHeight = -1;
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            Measure();
            SetLayoutInputForAxis(0, 0, 1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            Measure();
            var height = (_stacked ? _lineHeight * 2 + 6 : _lineHeight) + 14;
            SetLayoutInputForAxis(height, height, 0, 1);
        }

        private void Measure()
        {
            var required = 40f;
            _lineHeight = 36;
            for (var i = 0; i < _items.Length; i++)
            {
                var text = _items[i].GetComponent<TMP_Text>();
                _widths[i] = text != null ? text.preferredWidth + 4 : 80;
                if (text != null) _lineHeight = Mathf.Max(_lineHeight, Mathf.Ceil(text.preferredHeight) + 4);
                required += _widths[i] + (i == 0 ? 16 : 8);
            }

            _stacked = required > rectTransform.rect.width;
        }

        public override void SetLayoutHorizontal()
        {
            if (_items.Length == 0) return;
            Measure();
            var controlsWidth = 24f;
            for (var i = 1; i < _items.Length; i++) controlsWidth += _widths[i];
            SetChildAlongAxis(_items[0], 0, 20,
                Mathf.Max(0, rectTransform.rect.width - 40 - (_stacked ? 0 : controlsWidth + 16)));
            var x = _stacked ? 20 : rectTransform.rect.width - 20 - controlsWidth;
            for (var i = 1; i < _items.Length; i++)
            {
                SetChildAlongAxis(_items[i], 0, x, _widths[i]);
                x += _widths[i] + 8;
            }
        }

        public override void SetLayoutVertical()
        {
            for (var i = 0; i < _items.Length; i++)
                SetChildAlongAxis(_items[i], 1, _stacked && i > 0 ? _lineHeight + 13 : 7, _lineHeight);
        }
    }
}
