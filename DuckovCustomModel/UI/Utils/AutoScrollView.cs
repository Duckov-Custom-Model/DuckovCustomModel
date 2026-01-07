using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Utils
{
    public class AutoScrollView : MonoBehaviour
    {
        private readonly List<GameObject> _createdObjects = [];
        private readonly float _spacingHeight = 30f;
        private RectTransform? _contentRect;
        private bool _hasDuplicate;
        private bool _isScrolling;
        private bool _isUpdating;
        private float _maxHeight;
        private float _originalContentHeight;
        private CancellationTokenSource? _scrollCts;
        private ScrollRect? _scrollRect;

        private void OnDestroy()
        {
            _scrollCts?.Cancel();
            _scrollCts?.Dispose();
            RemoveDuplicate();
        }

        private void RemoveDuplicate()
        {
            if (!_hasDuplicate) return;

            foreach (var obj in _createdObjects.OfType<GameObject>())
                Destroy(obj);

            _createdObjects.Clear();
            _hasDuplicate = false;
        }

        private void CreateDuplicate()
        {
            if (_contentRect == null || _hasDuplicate) return;

            var originalChildCount = _contentRect.childCount;

            var spacing = new GameObject("Spacing", typeof(RectTransform));
            spacing.transform.SetParent(_contentRect, false);
            var spacingRect = spacing.GetComponent<RectTransform>();
            spacingRect.anchorMin = new Vector2(0, 0);
            spacingRect.anchorMax = new Vector2(1, 0);
            spacingRect.pivot = new Vector2(0.5f, 0);
            spacingRect.sizeDelta = new Vector2(0, _spacingHeight);
            spacingRect.anchoredPosition = Vector2.zero;

            var spacingLayout = spacing.AddComponent<LayoutElement>();
            spacingLayout.minHeight = _spacingHeight;
            spacingLayout.preferredHeight = _spacingHeight;
            spacingLayout.flexibleHeight = 0;

            _createdObjects.Add(spacing);

            for (var i = 0; i < originalChildCount; i++)
            {
                var original = _contentRect.GetChild(i);
                var duplicate = Instantiate(original.gameObject, _contentRect);
                duplicate.name = original.name + "_Duplicate";
                _createdObjects.Add(duplicate);
            }

            _hasDuplicate = true;
        }

        public void Initialize(ScrollRect scrollRect, RectTransform contentRect, float maxHeight)
        {
            _scrollRect = scrollRect;
            _contentRect = contentRect;
            _maxHeight = maxHeight;

            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.scrollSensitivity = 0;

            var image = _scrollRect.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;

            Refresh();
        }

        public void Refresh()
        {
            CheckContentAndUpdateAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid CheckContentAndUpdateAsync(CancellationToken cancellationToken)
        {
            if (_isUpdating || _contentRect == null || _scrollRect == null) return;

            _isUpdating = true;

            try
            {
                if (_hasDuplicate) RemoveDuplicate();

                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
                await UniTask.Yield(cancellationToken);

                var contentHeight = _contentRect.rect.height;
                var viewportHeight = _maxHeight;

                if (contentHeight > viewportHeight)
                {
                    _originalContentHeight = contentHeight;
                    CreateDuplicate();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
                    await UniTask.Yield(cancellationToken);

                    if (!_isScrolling)
                    {
                        _isScrolling = true;
                        _scrollCts?.Cancel();
                        _scrollCts?.Dispose();
                        _scrollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        AutoScrollAsync(_scrollCts.Token).Forget();
                    }
                }
                else
                {
                    if (_isScrolling)
                    {
                        _isScrolling = false;
                        _scrollCts?.Cancel();
                        _scrollCts?.Dispose();
                        _scrollCts = null;
                        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 1f;
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private async UniTaskVoid AutoScrollAsync(CancellationToken cancellationToken)
        {
            if (_scrollRect == null || _contentRect == null) return;

            const float scrollSpeed = 0.3f;
            var scrollTarget = _originalContentHeight + _spacingHeight;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_scrollRect == null || _contentRect == null) return;

                var contentHeight = _contentRect.rect.height;
                var viewportHeight = _maxHeight;
                var scrollableHeight = contentHeight - viewportHeight;

                if (scrollableHeight <= 0) return;

                var currentY = _contentRect.anchoredPosition.y;

                while (currentY < scrollTarget && !cancellationToken.IsCancellationRequested)
                {
                    if (_scrollRect == null || _contentRect == null) return;

                    currentY += scrollSpeed * Time.deltaTime * 100f;
                    _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x, currentY);
                    await UniTask.Yield(cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested) return;

                var resetY = currentY - scrollTarget;
                _contentRect.anchoredPosition = new Vector2(_contentRect.anchoredPosition.x, resetY);
            }
        }
    }
}
