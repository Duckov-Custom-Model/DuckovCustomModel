using System;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Tabs
{
    public class SettingsTab : Base.UIPanel
    {
        private Text? _animatorParamsLabel;
        private Toggle? _animatorParamsToggle;
        private Text? _keyButtonText;
        private Text? _keyLabel;

        private int _settingRowIndex;

        public bool IsWaitingForKeyInput { get; private set; }

        private static UIConfig? UIConfig => ModBehaviour.Instance?.UIConfig;

        private void Update()
        {
            if (IsWaitingForKeyInput) HandleKeyInputCapture();
        }

        protected override void OnDestroy()
        {
            Localization.OnLanguageChangedEvent -= OnLanguageChanged;
            base.OnDestroy();
        }

        public event Action<bool>? OnAnimatorParamsToggleChanged;

        protected override void CreatePanel()
        {
            PanelRoot = UIFactory.CreateImage("SettingsPanel", transform, new(0.08f, 0.1f, 0.12f, 0.95f));
            UIFactory.SetupRectTransform(PanelRoot, new(0, 0), new(1, 1), Vector2.zero);

            var outline = PanelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);
        }

        protected override void BuildContent()
        {
            if (PanelRoot == null) return;

            _settingRowIndex = 0;
            BuildSettingsContent();
            Localization.OnLanguageChangedEvent += OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            if (_keyButtonText != null && !IsWaitingForKeyInput)
                _keyButtonText.text = GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash);

            if (_keyLabel != null)
                _keyLabel.text = Localization.Hotkey;

            if (_animatorParamsLabel != null)
                _animatorParamsLabel.text = Localization.ShowAnimatorParameters;
        }

        private void BuildSettingsContent()
        {
            if (PanelRoot == null) return;

            var scrollView = UIFactory.CreateScrollView("SettingsScrollView", PanelRoot.transform, out var contentArea);
            UIFactory.SetupRectTransform(scrollView.gameObject, new(0, 0), new(1, 1), Vector2.zero);
            scrollView.GetComponent<RectTransform>().offsetMin = new(10, 10);
            scrollView.GetComponent<RectTransform>().offsetMax = new(-10, -10);

            contentArea.AddComponent<VerticalLayoutGroup>();
            contentArea.AddComponent<ContentSizeFitter>();

            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.sizeDelta = new(400, 0);

            var layoutGroup = contentArea.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            var sizeFitter = contentArea.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildKeySetting(contentArea);
            BuildAnimatorParamsToggle(contentArea);
        }

        private void BuildKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.Hotkey, 18, Color.white);
            _keyLabel = keyLabel.GetComponent<Text>();
            var keyLabelRect = keyLabel.GetComponent<RectTransform>();
            keyLabelRect.anchorMin = new(0, 0.5f);
            keyLabelRect.anchorMax = new(0, 0.5f);
            keyLabelRect.pivot = new(0, 0.5f);
            keyLabelRect.sizeDelta = new(0, 30);
            keyLabelRect.anchoredPosition = new(20, 0);
            var keyLabelSizeFitter = keyLabel.AddComponent<ContentSizeFitter>();
            keyLabelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            keyLabelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            var keyButtonRect = keyButton.GetComponent<RectTransform>();
            keyButtonRect.anchorMin = new(1, 0.5f);
            keyButtonRect.anchorMax = new(1, 0.5f);
            keyButtonRect.pivot = new(1, 0.5f);
            keyButtonRect.sizeDelta = new(100, 30);
            keyButtonRect.anchoredPosition = new(-20, 0);

            var keyButtonText = UIFactory.CreateText("Text", keyButton.transform,
                GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(keyButtonText, Vector2.zero, Vector2.one, Vector2.zero);
            _keyButtonText = keyButtonText.GetComponent<Text>();
        }

        private void BuildAnimatorParamsToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowAnimatorParameters, 18, Color.white);
            _animatorParamsLabel = label.GetComponent<Text>();
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.sizeDelta = new(0, 30);
            labelRect.anchoredPosition = new(20, 0);
            var labelSizeFitter = label.AddComponent<ContentSizeFitter>();
            labelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var configWindow = GetComponentInParent<ConfigWindow>();
            var toggle = UIFactory.CreateToggle("AnimatorParamsToggle", row.transform,
                configWindow?.IsAnimatorParamsWindowVisible() ?? false, SetAnimatorParamsWindowVisible);
            var toggleRect = toggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new(1, 0.5f);
            toggleRect.anchorMax = new(1, 0.5f);
            toggleRect.pivot = new(1, 0.5f);
            toggleRect.sizeDelta = new(20, 20);
            toggleRect.anchoredPosition = new(-20, 0);

            _animatorParamsToggle = toggle;
        }

        private GameObject CreateSettingRow(GameObject parent)
        {
            var row = new GameObject("SettingRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent.transform, false);

            var rowImage = row.GetComponent<Image>();
            var isEven = _settingRowIndex % 2 == 0;
            rowImage.color = isEven ? new(0.12f, 0.14f, 0.16f, 0.8f) : new(0.1f, 0.12f, 0.14f, 0.8f);
            _settingRowIndex++;

            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new(0, 1);
            rowRect.anchorMax = new(1, 1);
            rowRect.pivot = new(0.5f, 1);
            rowRect.sizeDelta = new(400, 50);
            rowRect.anchoredPosition = Vector2.zero;

            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 50;
            rowLayoutElement.minWidth = 400;
            rowLayoutElement.preferredWidth = 400;
            rowLayoutElement.flexibleWidth = 0;
            rowLayoutElement.flexibleHeight = 0;

            return row;
        }

        private void OnKeyButtonClicked()
        {
            if (UIConfig == null) return;
            IsWaitingForKeyInput = true;
            if (_keyButtonText != null)
                _keyButtonText.text = Localization.PressAnyKey;
        }

        private void HandleKeyInputCapture()
        {
            if (!IsWaitingForKeyInput || UIConfig == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                IsWaitingForKeyInput = false;
                if (_keyButtonText != null)
                    _keyButtonText.text = GetKeyCodeDisplayName(UIConfig.ToggleKey);
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Mouse0 || keyCode == KeyCode.Mouse1 || keyCode == KeyCode.Mouse2 ||
                        keyCode == KeyCode.Mouse3 || keyCode == KeyCode.Mouse4 || keyCode == KeyCode.Mouse5 ||
                        keyCode == KeyCode.Mouse6)
                        continue;

                    UIConfig.ToggleKey = keyCode;
                    ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                    IsWaitingForKeyInput = false;
                    if (_keyButtonText != null)
                        _keyButtonText.text = GetKeyCodeDisplayName(keyCode);
                    return;
                }
        }

        private static string GetKeyCodeDisplayName(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Backslash => "\\",
                KeyCode.Slash => "/",
                KeyCode.LeftBracket => "[",
                KeyCode.RightBracket => "]",
                KeyCode.Semicolon => ";",
                KeyCode.Quote => "'",
                KeyCode.Comma => ",",
                KeyCode.Period => ".",
                KeyCode.Equals => "=",
                KeyCode.Minus => "-",
                KeyCode.Plus => "+",
                KeyCode.LeftShift => "Left Shift",
                KeyCode.RightShift => "Right Shift",
                KeyCode.LeftControl => "Left Ctrl",
                KeyCode.RightControl => "Right Ctrl",
                KeyCode.LeftAlt => "Left Alt",
                KeyCode.RightAlt => "Right Alt",
                KeyCode.LeftCommand => "Left Cmd",
                KeyCode.RightCommand => "Right Cmd",
                KeyCode.LeftWindows => "Left Win",
                KeyCode.RightWindows => "Right Win",
                _ => keyCode.ToString(),
            };
        }

        public void SetAnimatorParamsWindowVisible(bool visible)
        {
            OnAnimatorParamsToggleChanged?.Invoke(visible);
        }
    }
}