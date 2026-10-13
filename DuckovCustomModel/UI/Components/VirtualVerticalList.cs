using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    internal sealed class VirtualVerticalList : MonoBehaviour
    {
        private readonly List<GameObject> _rows = new();
        private Action<GameObject, int>? _bind;
        private int _count;
        private Func<Transform, GameObject>? _create;
        private int _first = -1;
        private float _height;
        private RectOffset _padding = new();
        private bool _refreshing;
        private ScrollRect? _scroll;
        private float _spacing;
        private Vector2 _viewportSize = new(-1, -1);

        private void LateUpdate()
        {
            if (_scroll == null) return;
            var viewport = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
            if (_viewportSize != viewport.rect.size) RefreshVisible();
        }

        private void OnEnable()
        {
            RefreshVisible();
        }

        private void OnDestroy()
        {
            if (_scroll != null) _scroll.onValueChanged.RemoveListener(OnScroll);
        }

        internal void Initialize(ScrollRect scroll, float height, float spacing, RectOffset padding,
            Func<Transform, GameObject> create, Action<GameObject, int> bind)
        {
            _scroll = scroll;
            _height = height;
            _spacing = spacing;
            _padding = padding;
            _create = create;
            _bind = bind;
            scroll.onValueChanged.AddListener(OnScroll);
        }

        private void OnScroll(Vector2 position)
        {
            RefreshRows(false);
        }

        internal void SetCount(int count)
        {
            _count = Mathf.Max(0, count);
            if (_scroll == null || _scroll.content == null) return;
            var content = _scroll.content;
            var height = _padding.vertical + _count * _height + Mathf.Max(0, _count - 1) * _spacing;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            var viewport = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y, 0, Mathf.Max(0, height - viewport.rect.height));
            content.anchoredPosition = position;
            _scroll.StopMovement();
            RefreshVisible();
        }

        internal void RefreshVisible()
        {
            RefreshRows(true);
        }

        private void RefreshRows(bool force)
        {
            if (_refreshing || !isActiveAndEnabled || _scroll == null || _scroll.content == null ||
                _create == null || _bind == null) return;
            _refreshing = true;
            try
            {
                var viewport = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
                _viewportSize = viewport.rect.size;
                var capacity = _viewportSize.y <= 0
                    ? 0
                    : Mathf.Min(_count,
                        Mathf.CeilToInt(_viewportSize.y / (_height + _spacing)) + 3);
                var first = Mathf.Clamp(Mathf.FloorToInt((_scroll.content.anchoredPosition.y - _padding.top) /
                                                         (_height + _spacing)) - 1, 0, Mathf.Max(0, _count - capacity));
                if (!force && first == _first && capacity == _rows.Count) return;
                _first = first;
                while (_rows.Count > capacity)
                {
                    var index = _rows.Count - 1;
                    _rows[index].SetActive(false);
                    Destroy(_rows[index]);
                    _rows.RemoveAt(index);
                }

                while (_rows.Count < capacity) _rows.Add(_create(_scroll.content));
                for (var i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    row.SetActive(false);
                    var rect = (RectTransform)row.transform;
                    rect.anchorMin = new(0, 1);
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new(0, 1);
                    rect.sizeDelta = new(-_padding.horizontal, _height);
                    rect.anchoredPosition = new(_padding.left, -_padding.top - (first + i) * (_height + _spacing));
                    _bind(row, first + i);
                    row.SetActive(true);
                }
            }
            finally
            {
                _refreshing = false;
            }
        }
    }
}
