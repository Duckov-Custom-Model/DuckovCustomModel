using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class MultiSelectDropdown : MonoBehaviour, IPointerClickHandler
    {
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly HashSet<string> _selectedValues = new();
        private GameObject? _button;
        private GameObject? _buttonText;
        private Canvas? _canvas;
        private RectTransform? _content;
        private GameObject? _dropdownPanel;
        private Func<string, string>? _getDisplayName;
        private Action? _onSelectionChanged;
        private string[] _options = [];
        private VirtualVerticalList? _virtualList;

        private void Update()
        {
            if (_dropdownPanel == null || !_dropdownPanel.activeSelf ||
                !InputCompatibility.GetMouseButtonDown(0)) return;
            var mousePos = InputCompatibility.MousePosition;
            var panelRect = _dropdownPanel.GetComponent<RectTransform>();
            var buttonRect = GetComponent<RectTransform>();

            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos, null) &&
                !RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePos, null))
                _dropdownPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (_dropdownPanel != null) _dropdownPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_dropdownPanel != null) Destroy(_dropdownPanel);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dropdownPanel == null || !_dropdownPanel.activeSelf) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(_dropdownPanel.GetComponent<RectTransform>(),
                    eventData.position, null) &&
                !RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(),
                    eventData.position, null))
                _dropdownPanel.SetActive(false);
        }

        public void Initialize(string[] options, HashSet<string> initialSelection,
            Func<string, string>? getDisplayName = null, Action? onSelectionChanged = null)
        {
            _options = options;
            _selectedValues.Clear();
            foreach (var value in initialSelection)
                _selectedValues.Add(value);
            _getDisplayName = getDisplayName;
            _onSelectionChanged = onSelectionChanged;
            CreateButton();
            UpdateButtonText();
            _virtualList?.SetCount(_options.Length);
            if (_dropdownPanel != null && _dropdownPanel.activeSelf)
            {
                UpdateDropdownPosition();
                _virtualList?.RefreshVisible();
            }
        }

        public HashSet<string> GetSelectedValues()
        {
            return [.. _selectedValues];
        }

        public void SetSelectedValues(HashSet<string> values)
        {
            _selectedValues.Clear();
            foreach (var value in values)
                _selectedValues.Add(value);
            UpdateButtonText();
            _virtualList?.RefreshVisible();
        }

        private void CreateButton()
        {
            if (_button != null) return;

            _button = UIFactory.CreateButton("Button", transform, ToggleDropdown, new Color(0.1f, 0.12f, 0.15f, 0.9f));
            var buttonRect = _button.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            _buttonText = UIFactory.CreateText("Text", _button.transform, "", 14, Color.white);
            var buttonTextRect = _buttonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = new(0, 0);
            buttonTextRect.anchorMax = new(1, 1);
            buttonTextRect.offsetMin = new(5, 0);
            buttonTextRect.offsetMax = new(-20, 0);
            var text = _buttonText.GetComponent<TMP_Text>();
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            UpdateButtonText();

            var arrowObj =
                UIFactory.CreateText("Arrow", _button.transform, "▼", 12, Color.white, TextAnchor.MiddleRight);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new(1, 0);
            arrowRect.anchorMax = new(1, 1);
            arrowRect.pivot = new(1, 0.5f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrowRect.sizeDelta = new(15, 0);
            arrowRect.offsetMin = new(-15, 0);
            arrowRect.offsetMax = new(-5, 0);
        }

        private void CreateDropdownPanel()
        {
            if (_dropdownPanel != null) return;

            _canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (_canvas == null) return;
            _dropdownPanel = UIFactory.CreateImage("DropdownPanel", _canvas.transform, UITheme.Surface);
            var popupCanvas = _dropdownPanel.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = _canvas.sortingOrder + 10;
            _dropdownPanel.AddComponent<GraphicRaycaster>();
            _dropdownPanel.SetActive(false);

            var outline = _dropdownPanel.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            var scroll = UIFactory.CreateScrollView("Options", _dropdownPanel.transform, out var content);
            UIFactory.SetupRectTransform(scroll.gameObject, Vector2.zero, Vector2.one,
                offsetMin: new(4, 4), offsetMax: new(-4, -4));
            _content = content.GetComponent<RectTransform>();

            _virtualList = scroll.gameObject.AddComponent<VirtualVerticalList>();
            _virtualList.Initialize(scroll, 30, 2, new(5, 5, 5, 5), CreateOptionRow, BindOptionRow);
            _virtualList.SetCount(_options.Length);
        }

        private GameObject CreateOptionRow(Transform parent)
        {
            var toggleObj = new GameObject("Option", typeof(RectTransform), typeof(Image),
                typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);

            var toggle = toggleObj.GetComponent<Toggle>();
            var rowImage = toggleObj.GetComponent<Image>();
            rowImage.color = UITheme.Control;
            toggle.targetGraphic = rowImage;
            var row = toggleObj.AddComponent<MultiSelectOptionRow>();
            row.Toggle = toggle;
            toggle.onValueChanged.AddListener(value => OnToggleChanged(row.Key, value));

            var checkboxVar = new GameObject("Checkbox", typeof(RectTransform), typeof(Image));
            checkboxVar.transform.SetParent(toggleObj.transform, false);

            var checkboxRect = checkboxVar.GetComponent<RectTransform>();
            checkboxRect.anchorMin = new(0, 0.5f);
            checkboxRect.anchorMax = new(0, 0.5f);
            checkboxRect.pivot = new(0.5f, 0.5f);
            checkboxRect.sizeDelta = new(20, 20);
            checkboxRect.anchoredPosition = new(10, 0);

            var bgImage = checkboxVar.GetComponent<Image>();
            bgImage.color = new(0.2f, 0.2f, 0.2f, 1);
            bgImage.raycastTarget = false;

            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(checkboxVar.transform, false);
            var checkImage = checkmark.GetComponent<Image>();
            checkImage.color = UITheme.Highlight;
            checkImage.raycastTarget = false;

            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.pivot = new(0.5f, 0.5f);
            checkRect.anchoredPosition = Vector2.zero;
            checkRect.sizeDelta = Vector2.zero;
            checkRect.offsetMin = new(4, 4);
            checkRect.offsetMax = new(-4, -4);

            toggle.graphic = checkImage;

            var labelObj = new GameObject("Label", typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(toggleObj.transform, false);
            var labelText = labelObj.GetComponent<TextMeshProUGUI>();
            row.Label = labelText;
            labelText.fontSize = 14;
            labelText.raycastTarget = false;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Ellipsis;

            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0);
            labelRect.anchorMax = new(1, 1);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new(40, 0);
            labelRect.offsetMax = new(-5, 0);
            return toggleObj;
        }

        private void BindOptionRow(GameObject item, int index)
        {
            var row = item.GetComponent<MultiSelectOptionRow>();
            row.Key = _options[index];
            row.Label.text = _getDisplayName != null ? _getDisplayName(row.Key) : row.Key;
            row.Toggle.SetIsOnWithoutNotify(_selectedValues.Contains(row.Key));
        }

        private void UpdateButtonText()
        {
            if (_buttonText == null) return;

            var textMeshProUGUI = _buttonText.GetComponent<TextMeshProUGUI>();
            if (textMeshProUGUI == null) return;

            var selectedCount = _selectedValues.Count;
            if (selectedCount == 0)
            {
                textMeshProUGUI.text = Localization.None;
            }
            else if (selectedCount == _options.Length)
            {
                textMeshProUGUI.text = Localization.AnimatorParamFilterAll;
            }
            else if (selectedCount == 1)
            {
                var selected = _selectedValues.First();
                var displayName = _getDisplayName != null ? _getDisplayName(selected) : selected;
                textMeshProUGUI.text = displayName;
            }
            else
            {
                textMeshProUGUI.text = $"{selectedCount}/{_options.Length}";
            }
        }

        private void ToggleDropdown()
        {
            if (_dropdownPanel == null) CreateDropdownPanel();
            if (_dropdownPanel == null) return;

            _dropdownPanel.SetActive(!_dropdownPanel.activeSelf);
            if (!_dropdownPanel.activeSelf) return;

            _dropdownPanel.transform.SetAsLastSibling();
            UpdateDropdownPosition();
            _virtualList?.RefreshVisible();
        }

        private void UpdateDropdownPosition()
        {
            if (_dropdownPanel == null) return;

            if (_content == null || _canvas == null) return;
            var canvasRect = (RectTransform)_canvas.transform;
            var buttonRect = GetComponent<RectTransform>();
            var panelRect = _dropdownPanel.GetComponent<RectTransform>();
            buttonRect.GetWorldCorners(_corners);
            var bottomLeft = canvasRect.InverseTransformPoint(_corners[0]);
            var topLeft = canvasRect.InverseTransformPoint(_corners[1]);
            var bottomRight = canvasRect.InverseTransformPoint(_corners[3]);
            var bounds = canvasRect.rect;
            var width = Mathf.Min(Mathf.Max(180, bottomRight.x - bottomLeft.x), bounds.width - 16);
            var below = bottomLeft.y - bounds.yMin - 8;
            var above = bounds.yMax - topLeft.y - 8;
            var showAbove = below < Mathf.Min(180, _content.rect.height + 8) && above > below;
            var height = Mathf.Min(_content.rect.height + 8, 320, Mathf.Max(32, showAbove ? above : below));
            var left = Mathf.Clamp(bottomLeft.x, bounds.xMin + 8, bounds.xMax - width - 8);
            panelRect.anchorMin = canvasRect.pivot;
            panelRect.anchorMax = canvasRect.pivot;
            panelRect.pivot = new(0, 1);
            panelRect.sizeDelta = new(width, height);
            panelRect.anchoredPosition = new(left, showAbove ? topLeft.y + height : bottomLeft.y);
        }

        private void OnToggleChanged(string key, bool value)
        {
            if (value)
                _selectedValues.Add(key);
            else
                _selectedValues.Remove(key);

            UpdateButtonText();
            _onSelectionChanged?.Invoke();
        }
    }

    internal sealed class MultiSelectOptionRow : MonoBehaviour
    {
        internal string Key = string.Empty;
        internal TMP_Text Label = null!;
        internal Toggle Toggle = null!;
    }
}
