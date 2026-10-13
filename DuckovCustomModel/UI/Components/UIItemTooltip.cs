using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DuckovCustomModel.UI.Components
{
    internal sealed class UIItemTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static UIItemTooltip? _open;
        private RectTransform? _canvasRect;
        private GameObject? _details;

        private Vector2 _pointerPosition;
        private Action? _preload;
        private TextMeshProUGUI? _text;

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            Hide();
            if (_details != null) Destroy(_details);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_preload != null) Invoke(nameof(Preload), .25f);
            _pointerPosition = eventData.position;
            Invoke(nameof(ShowTooltip), .2f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        private void ShowTooltip()
        {
            if (!AttachTooltip()) return;
            var canvasRect = _canvasRect;
            if (canvasRect == null || _details == null) return;
            if (_open != null && _open != this) _open.Hide();
            _open = this;
            var rect = (RectTransform)_details.transform;
            var canvas = canvasRect.GetComponent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, _pointerPosition,
                    camera, out var point))
            {
                var bounds = canvasRect.rect;
                var x = point.x + 16f;
                if (x + rect.rect.width > bounds.xMax - 8f) x = point.x - rect.rect.width - 16f;
                rect.anchoredPosition = new(
                    Mathf.Clamp(x, bounds.xMin + 8f, bounds.xMax - rect.rect.width - 8f),
                    Mathf.Clamp(point.y + 8f, bounds.yMin + rect.rect.height + 8f, bounds.yMax - 8f));
            }

            rect.SetAsLastSibling();
            _details.SetActive(true);
        }

        internal void Initialize(GameObject details, TextMeshProUGUI text, Action? preload = null)
        {
            _preload = preload;
            _details = details;
            _text = text;
        }

        private bool AttachTooltip()
        {
            if (_details == null || _text == null) return false;
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null) return false;
            _canvasRect = canvas.GetComponent<RectTransform>();
            _details.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)_details.transform;
            var width = Mathf.Min(320f, _canvasRect.rect.width - 20f);
            var height = Mathf.Clamp(_text.GetPreferredValues(_text.text, width - 14f, 10000f).y + 14f,
                48f, Mathf.Max(48f, _canvasRect.rect.height - 20f));
            rect.sizeDelta = new(width, height);
            return true;
        }

        private void Preload()
        {
            _preload?.Invoke();
        }

        private void Hide()
        {
            CancelInvoke(nameof(Preload));
            CancelInvoke(nameof(ShowTooltip));
            if (_details != null) _details.SetActive(false);
            if (_open == this) _open = null;
        }
    }
}
