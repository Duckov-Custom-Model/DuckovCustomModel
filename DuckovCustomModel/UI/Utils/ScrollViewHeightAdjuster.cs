using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Utils
{
    public class ScrollViewHeightAdjuster : MonoBehaviour
    {
        private RectTransform? _contentRect;
        private bool _isUpdating;
        private LayoutElement? _layoutElement;
        private float _maxHeight;
        private float _minHeight;
        private ScrollRect? _scrollRect;
        private TextMeshProUGUI? _textComponent;

        private void OnDestroy()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Canvas.willRenderCanvases -= OnCanvasWillRender;
        }

        public void Initialize(ScrollRect scrollRect, GameObject content, float minHeight, float maxHeight)
        {
            _scrollRect = scrollRect;
            _contentRect = content.GetComponent<RectTransform>();
            _textComponent = content.GetComponentInChildren<TextMeshProUGUI>();
            _minHeight = minHeight;
            _maxHeight = maxHeight;
            _layoutElement = GetComponent<LayoutElement>();

            if (_textComponent != null) TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            Canvas.willRenderCanvases += OnCanvasWillRender;
        }

        private void OnTextChanged(Object obj)
        {
            if (obj == _textComponent && !_isUpdating) UpdateHeight();
        }

        private void OnCanvasWillRender()
        {
            if (!_isUpdating && _contentRect != null && _contentRect.rect.height > 0) UpdateHeight();
        }

        public void UpdateHeight()
        {
            if (_contentRect == null || _layoutElement == null) return;
            if (_isUpdating) return;

            _isUpdating = true;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);

            var contentHeight = _contentRect.rect.height;

            if (contentHeight <= 0.1f && _textComponent != null)
            {
                _textComponent.ForceMeshUpdate();
                var preferredHeight = _textComponent.preferredHeight;
                contentHeight = preferredHeight;
            }

            _layoutElement.preferredHeight = Mathf.Min(contentHeight, _maxHeight);

            _isUpdating = false;
        }
    }
}
