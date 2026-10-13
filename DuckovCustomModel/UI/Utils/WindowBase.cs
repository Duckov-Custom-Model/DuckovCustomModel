using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Utils
{
    public class WindowBase : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private Vector2 _initialMousePos;
        private Vector2 _initialPosition;
        private Vector2 _initialSize;
        private bool _isDragging;
        private bool _isResizing;
        private Vector2 _maxSize = new(1920, 1080);
        private Vector2 _minSize = new(300, 200);
        private GameObject? _resizeHandle;
        private Image? _resizeHandleImage;
        private RectTransform? _windowRectTransform;

        private void Start()
        {
            _windowRectTransform = GetComponent<RectTransform>();
            if (_windowRectTransform == null)
            {
                var parent = transform.parent;
                while (parent != null && _windowRectTransform == null)
                {
                    _windowRectTransform = parent.GetComponent<RectTransform>();
                    if (_windowRectTransform != null) break;
                    parent = parent.parent;
                }
            }

            CreateResizeHandle();
            ClampToParent();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_windowRectTransform == null) return;

            _initialMousePos = eventData.position;
            _initialSize = _windowRectTransform.rect.size;
            _initialPosition = _windowRectTransform.anchoredPosition;

            _isResizing = IsResizeHandle(eventData.position);
            var title = _windowRectTransform.Find("TitleBar") as RectTransform;
            _isDragging = !_isResizing && (title == null ||
                                           RectTransformUtility.RectangleContainsScreenPoint(title, eventData.position,
                                               eventData.pressEventCamera));
            if (_isDragging || _isResizing) _windowRectTransform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_windowRectTransform == null) return;

            if (_isResizing)
                ResizeWindow(eventData);
            else if (_isDragging)
                DragWindow(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _isResizing = false;
        }

        private void CreateResizeHandle()
        {
            if (_windowRectTransform == null) return;

            _resizeHandle = new("ResizeHandle", typeof(RectTransform), typeof(Image));
            _resizeHandle.transform.SetParent(_windowRectTransform, false);

            var handleRect = _resizeHandle.GetComponent<RectTransform>();
            handleRect.anchorMin = new(1, 0);
            handleRect.anchorMax = new(1, 0);
            handleRect.pivot = new(1, 0);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new(20f, 20f);

            _resizeHandleImage = _resizeHandle.GetComponent<Image>();
            _resizeHandleImage.color = new(0.5f, 0.5f, 0.5f, 0.3f);
            _resizeHandleImage.raycastTarget = true;

            var dragHandler = _resizeHandle.AddComponent<ResizeHandleDragHandler>();
            dragHandler.Initialize(this);

            var eventTrigger = _resizeHandle.AddComponent<EventTrigger>();

            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener(_ => OnResizeHandlePointerEnter());
            eventTrigger.triggers.Add(pointerEnter);

            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener(_ => OnResizeHandlePointerExit());
            eventTrigger.triggers.Add(pointerExit);
        }

        internal void HandleResizeHandleDrag(PointerEventData eventData)
        {
            if (_windowRectTransform == null) return;

            if (!_isResizing)
            {
                _initialMousePos = eventData.position;
                _initialSize = _windowRectTransform.rect.size;
                _initialPosition = _windowRectTransform.anchoredPosition;
                _isResizing = true;
            }

            ResizeWindow(eventData);
        }

        internal void HandleResizeHandleEndDrag(PointerEventData eventData)
        {
            _isResizing = false;
        }

        private void OnResizeHandlePointerEnter()
        {
            if (_resizeHandleImage != null) _resizeHandleImage.color = new(0.7f, 0.7f, 0.7f, 0.6f);
        }

        private void OnResizeHandlePointerExit()
        {
            if (_resizeHandleImage != null) _resizeHandleImage.color = new(0.5f, 0.5f, 0.5f, 0.3f);
        }

        public void SetMinSize(Vector2 minSize)
        {
            _minSize = minSize;
        }

        public void SetMaxSize(Vector2 maxSize)
        {
            _maxSize = maxSize;
        }

        private bool IsResizeHandle(Vector2 screenPosition)
        {
            if (_resizeHandle == null) return false;

            var handleRect = _resizeHandle.GetComponent<RectTransform>();
            if (handleRect == null) return false;

            return RectTransformUtility.RectangleContainsScreenPoint(handleRect, screenPosition, null);
        }

        private void DragWindow(PointerEventData eventData)
        {
            if (_windowRectTransform == null) return;

            var screenDelta = eventData.position - _initialMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRectTransform.parent as RectTransform,
                _initialMousePos + screenDelta,
                eventData.pressEventCamera,
                out var newPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRectTransform.parent as RectTransform,
                _initialMousePos,
                eventData.pressEventCamera,
                out var oldPoint);
            var localDelta = newPoint - oldPoint;
            _windowRectTransform.anchoredPosition = _initialPosition + localDelta;
            ClampToParent();
        }

        private void ResizeWindow(PointerEventData eventData)
        {
            if (_windowRectTransform == null) return;

            var screenDelta = eventData.position - _initialMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRectTransform.parent as RectTransform,
                _initialMousePos + screenDelta,
                eventData.pressEventCamera,
                out var newPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRectTransform.parent as RectTransform,
                _initialMousePos,
                eventData.pressEventCamera,
                out var oldPoint);
            var localDelta = newPoint - oldPoint;

            var newSize = _initialSize;
            var newPosition = _initialPosition;

            var available = _windowRectTransform.parent is RectTransform parent ? parent.rect.size : _maxSize;
            var maximum = Vector2.Min(_maxSize, available);
            var minimum = Vector2.Min(_minSize, maximum);
            newSize.x = Mathf.Clamp(_initialSize.x + localDelta.x, minimum.x, maximum.x);
            newSize.y = Mathf.Clamp(_initialSize.y - localDelta.y, minimum.y, maximum.y);
            newPosition.x = _initialPosition.x + (newSize.x - _initialSize.x) * _windowRectTransform.pivot.x;
            newPosition.y = _initialPosition.y - (newSize.y - _initialSize.y) * (1 - _windowRectTransform.pivot.y);

            _windowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            _windowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);
            _windowRectTransform.anchoredPosition = newPosition;
            ClampToParent();
        }

        private void ClampToParent()
        {
            if (_windowRectTransform == null || _windowRectTransform.parent is not RectTransform parent) return;
            var size = Vector2.Min(_windowRectTransform.rect.size, parent.rect.size);
            _windowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _windowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            var position = _windowRectTransform.localPosition;
            position.x = Mathf.Clamp(position.x, parent.rect.xMin + size.x * _windowRectTransform.pivot.x,
                parent.rect.xMax - size.x * (1 - _windowRectTransform.pivot.x));
            position.y = Mathf.Clamp(position.y, parent.rect.yMin + size.y * _windowRectTransform.pivot.y,
                parent.rect.yMax - size.y * (1 - _windowRectTransform.pivot.y));
            _windowRectTransform.localPosition = position;
        }

        private class ResizeHandleDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
        {
            private WindowBase? _windowBase;

            public void OnBeginDrag(PointerEventData eventData)
            {
                _windowBase?.OnBeginDrag(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                _windowBase?.HandleResizeHandleDrag(eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                _windowBase?.HandleResizeHandleEndDrag(eventData);
            }

            public void Initialize(WindowBase windowBase)
            {
                _windowBase = windowBase;
            }
        }
    }
}
