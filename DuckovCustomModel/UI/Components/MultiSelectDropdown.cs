using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class MultiSelectDropdown : MonoBehaviour, IPointerClickHandler
    {
        private readonly HashSet<string> _selectedValues = new();
        private readonly Dictionary<string, Toggle> _toggles = new();
        private GameObject? _button;
        private GameObject? _buttonText;
        private GameObject? _dropdownPanel;
        private Func<string, string>? _getDisplayName;
        private Action? _onSelectionChanged;
        private string[] _options = [];

        private void Update()
        {
            if (_dropdownPanel == null || !_dropdownPanel.activeSelf || !Input.GetMouseButtonDown(0)) return;
            var mousePos = Input.mousePosition;
            var panelRect = _dropdownPanel.GetComponent<RectTransform>();
            var buttonRect = GetComponent<RectTransform>();

            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos, null) &&
                !RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePos, null))
                _dropdownPanel.SetActive(false);
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
        }

        public HashSet<string> GetSelectedValues()
        {
            return [.._selectedValues];
        }

        public void SetSelectedValues(HashSet<string> values)
        {
            _selectedValues.Clear();
            foreach (var value in values)
                _selectedValues.Add(value);
            UpdateButtonText();
            if (_dropdownPanel == null) return;
            foreach (var (key, toggle) in _toggles)
                toggle.isOn = _selectedValues.Contains(key);
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
            buttonTextRect.anchorMin = new Vector2(0, 0);
            buttonTextRect.anchorMax = new Vector2(1, 1);
            buttonTextRect.offsetMin = new Vector2(5, 0);
            buttonTextRect.offsetMax = new Vector2(-20, 0);
            UpdateButtonText();

            var arrowObj =
                UIFactory.CreateText("Arrow", _button.transform, "▼", 12, Color.white, TextAnchor.MiddleRight);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrowRect.sizeDelta = new Vector2(15, 0);
            arrowRect.offsetMin = new Vector2(-15, 0);
            arrowRect.offsetMax = new Vector2(-5, 0);
        }

        private void CreateDropdownPanel()
        {
            if (_dropdownPanel != null) return;

            _dropdownPanel = UIFactory.CreateImage("DropdownPanel", transform, new Color(0.1f, 0.12f, 0.15f, 0.95f));
            _dropdownPanel.SetActive(false);

            var outline = _dropdownPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new Vector2(2, -2);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(_dropdownPanel.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(5, 5);
            contentRect.offsetMax = new Vector2(-5, -5);

            var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 2f;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            foreach (var option in _options)
            {
                var toggleObj = new GameObject($"Toggle_{option}", typeof(RectTransform), typeof(Toggle));
                toggleObj.transform.SetParent(content.transform, false);

                var toggleRect = toggleObj.GetComponent<RectTransform>();
                toggleRect.anchorMin = new Vector2(0, 1);
                toggleRect.anchorMax = new Vector2(1, 1);
                toggleRect.pivot = new Vector2(0.5f, 1);
                toggleRect.sizeDelta = new Vector2(0, 30);

                var toggle = toggleObj.GetComponent<Toggle>();
                toggle.isOn = _selectedValues.Contains(option);
                toggle.onValueChanged.AddListener(OnToggleChanged);
                _toggles[option] = toggle;

                var layoutElement = toggleObj.AddComponent<LayoutElement>();
                layoutElement.minHeight = 30;
                layoutElement.preferredHeight = 30;
                layoutElement.flexibleHeight = 0;

                var checkboxVar = new GameObject("Checkbox", typeof(RectTransform), typeof(Image));
                checkboxVar.transform.SetParent(toggleObj.transform, false);

                var checkboxRect = checkboxVar.GetComponent<RectTransform>();
                checkboxRect.anchorMin = new Vector2(0, 0.5f);
                checkboxRect.anchorMax = new Vector2(0, 0.5f);
                checkboxRect.pivot = new Vector2(0.5f, 0.5f);
                checkboxRect.sizeDelta = new Vector2(20, 20);
                checkboxRect.anchoredPosition = new Vector2(10, 0);

                var bgImage = checkboxVar.GetComponent<Image>();
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1);
                toggle.targetGraphic = bgImage;

                var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkmark.transform.SetParent(checkboxVar.transform, false);
                var checkImage = checkmark.GetComponent<Image>();
                checkImage.color = new Color(0.2f, 0.8f, 0.2f, 1);

                var checkRect = checkmark.GetComponent<RectTransform>();
                checkRect.anchorMin = Vector2.zero;
                checkRect.anchorMax = Vector2.one;
                checkRect.pivot = new Vector2(0.5f, 0.5f);
                checkRect.anchoredPosition = Vector2.zero;
                checkRect.sizeDelta = Vector2.zero;
                checkRect.offsetMin = new Vector2(4, 4);
                checkRect.offsetMax = new Vector2(-4, -4);

                toggle.graphic = checkImage;

                var labelObj = new GameObject("Label", typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(toggleObj.transform, false);
                var labelText = labelObj.GetComponent<TextMeshProUGUI>();
                var displayName = _getDisplayName != null ? _getDisplayName(option) : option;
                labelText.text = displayName;
                labelText.fontSize = 14;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.MidlineLeft;
                labelText.enableWordWrapping = false;
                labelText.overflowMode = TextOverflowModes.Ellipsis;

                var labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0);
                labelRect.anchorMax = new Vector2(1, 1);
                labelRect.pivot = new Vector2(0, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = new Vector2(40, 0);
                labelRect.offsetMax = new Vector2(-5, 0);
            }

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
        }

        private void UpdateDropdownPosition()
        {
            if (_dropdownPanel == null) return;

            var content = _dropdownPanel.transform.Find("Content");
            if (content == null) return;

            var contentRect = content.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            var buttonRect = GetComponent<RectTransform>();
            var panelRect = _dropdownPanel.GetComponent<RectTransform>();

            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.anchoredPosition = new Vector2(0, -buttonRect.sizeDelta.y + 15);

            var contentSize = contentRect.rect.height;
            panelRect.sizeDelta = new Vector2(buttonRect.rect.width, contentSize + 10);
        }

        private void OnToggleChanged(bool value)
        {
            foreach (var (key, toggle) in _toggles)
                if (toggle.isOn)
                    _selectedValues.Add(key);
                else
                    _selectedValues.Remove(key);

            UpdateButtonText();
            _onSelectionChanged?.Invoke();
        }
    }
}
