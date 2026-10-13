using System;
using System.Linq;
using System.Text;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Tabs
{
    public class SettingsTab : Base.UIPanel
    {
        private GameObject? _animatorParamsKeyButton;
        private Toggle? _animatorParamsToggle;
        private GameObject? _changelogText;
        private TMP_Dropdown? _dcmButtonAnchorDropdown;
        private TMP_InputField? _dcmButtonOffsetXInput;
        private TMP_InputField? _dcmButtonOffsetYInput;
        private string? _displayedChangelog;
        private GameObject? _downloadButtonsContainer;
        private string? _downloadSignature;
        private bool _isWaitingForAnimatorParamsKeyInput;
        private bool _isWaitingForModifierKey1Input;
        private bool _isWaitingForModifierKey2Input;

        private bool _isWaitingForUIKeyInput;
        private bool _isWaitingForYsmRadialMenuKeyInput;
        private GameObject? _keyButton;
        private float _lastUpdateInfoRefreshTime;
        private GameObject? _modifierKey1Button;
        private GameObject? _modifierKey2Button;

        private LocalizedText? _updateCheckButtonText;
        private LocalizedText? _updateInfoLocalizedText;
        private GameObject? _ysmRadialMenuKeyButton;

        private static UIConfig? UIConfig => ModEntry.UIConfig;

        public bool IsWaitingForKeyInput => _isWaitingForUIKeyInput || _isWaitingForAnimatorParamsKeyInput ||
                                            _isWaitingForModifierKey1Input || _isWaitingForModifierKey2Input ||
                                            _isWaitingForYsmRadialMenuKeyInput;

        private void Update()
        {
            if (IsWaitingForKeyInput)
                HandleKeyInputCapture();

            if (_updateInfoLocalizedText == null || !(Time.unscaledTime - _lastUpdateInfoRefreshTime > 30f)) return;
            _lastUpdateInfoRefreshTime = Time.unscaledTime;
            RefreshUpdateInfo();
        }

        private void OnDisable()
        {
            _isWaitingForUIKeyInput = false;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey1Input = false;
            _isWaitingForModifierKey2Input = false;
            _isWaitingForYsmRadialMenuKeyInput = false;
            RefreshAllKeyButtons();
        }

        protected override void OnDestroy()
        {
            UpdateChecker.OnUpdateCheckCompleted -= OnUpdateCheckCompleted;
            base.OnDestroy();
        }

        protected override void OnShow()
        {
            RefreshUpdateInfo();
        }

        public void RefreshAnimatorParamsToggleState(bool visible)
        {
            if (_animatorParamsToggle == null) return;
            if (_animatorParamsToggle.isOn != visible) _animatorParamsToggle.isOn = visible;
        }

        public event Action<bool>? OnAnimatorParamsToggleChanged;

        protected override void CreatePanel()
        {
            PanelRoot = UIFactory.CreateImage("SettingsPanel", transform, new(0.08f, 0.1f, 0.12f, 0.95f));
            UIFactory.SetupRectTransform(PanelRoot, new(0, 0), new(1, 1), Vector2.zero);

            PanelRoot.GetComponent<Image>().raycastTarget = false;
        }

        protected override void BuildContent()
        {
            if (PanelRoot == null) return;

            BuildSettingsContent();
        }

        private void BuildSettingsContent()
        {
            if (PanelRoot == null) return;

            var scrollView = UIFactory.CreateScrollView("SettingsScrollView", PanelRoot.transform, out var contentArea);
            UIFactory.SetupRectTransform(scrollView.gameObject, new(0, 0), new(1, 1), offsetMin: new(10, 10),
                offsetMax: new(-10, -10));

            var scrollbar = UIFactory.CreateScrollbar(scrollView, 6f, false);
            scrollbar.transform.SetParent(scrollView.transform, false);

            UIFactory.SetupVerticalLayoutGroup(contentArea, 8f, new(12, 22, 12, 12),
                TextAnchor.UpperLeft, true, true, true);
            UIFactory.SetupContentSizeFitter(contentArea, ContentSizeFitter.FitMode.Unconstrained);

            BuildSectionHeading(contentArea, () => Localization.HotkeySettings);
            BuildKeySetting(contentArea);
            BuildYsmRadialMenuKeySetting(contentArea);
            BuildModifierKeySettings(contentArea);
            BuildSectionHeading(contentArea, () => Localization.Settings);
            BuildShowDCMButtonToggle(contentArea);
            BuildDCMButtonPositionSettings(contentArea);
            BuildSectionHeading(contentArea, () => Localization.AnimatorSettings);
            BuildAnimatorParamsKeySetting(contentArea);
            BuildAnimatorParamsToggle(contentArea);
            var updates = UIFactory.CreateImage("UpdatesPanel", PanelRoot.transform, UITheme.SurfaceAlternate);
            updates.AddComponent<RectMask2D>();
            UIFactory.SetupVerticalLayoutGroup(updates, 10f, new(16, 16, 12, 12),
                TextAnchor.UpperLeft, true, true, true);
            BuildUpdateCheckSection(updates);
            PanelRoot.AddComponent<SettingsColumnsLayout>().Initialize(
                (RectTransform)scrollView.transform, (RectTransform)updates.transform);

            if (contentArea != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea.GetComponent<RectTransform>());
        }

        private void BuildKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.Hotkey, 18, Color.white);
            SetupSettingLabel(keyLabel, 140);
            UIFactory.SetLocalizedText(keyLabel, () => Localization.Hotkey);

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            _keyButton = keyButton;
            SetupSettingControl(keyButton, new(100, 30));

            var keyButtonText = UIFactory.CreateText("Text", keyButton.transform,
                GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(keyButtonText);
            UIFactory.SetLocalizedText(keyButtonText, () =>
                _isWaitingForUIKeyInput
                    ? Localization.PressAnyKey
                    : GetKeyCodeDisplayName(UIConfig?.ToggleKey ?? KeyCode.Backslash));
        }

        private void BuildAnimatorParamsKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.AnimatorParamsHotkey, 18, Color.white);
            SetupSettingLabel(keyLabel, 210);
            UIFactory.SetLocalizedText(keyLabel, () => Localization.AnimatorParamsHotkey);

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnAnimatorParamsKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            _animatorParamsKeyButton = keyButton;
            SetupSettingControl(keyButton, new(100, 30));

            var clearButton = UIFactory.CreateButton("ClearButton", row.transform, OnAnimatorParamsClearButtonClicked,
                new(0.3f, 0.2f, 0.2f, 1));
            SetupSettingControl(clearButton, new(60, 30), -20f - 100f - 8f);

            var clearButtonText = UIFactory.CreateText("Text", clearButton.transform,
                Localization.Clear, 16, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(clearButtonText);
            UIFactory.SetLocalizedText(clearButtonText, () => Localization.Clear);

            var keyButtonText = UIFactory.CreateText("Text", keyButton.transform,
                GetKeyCodeDisplayName(UIConfig?.AnimatorParamsToggleKey ?? KeyCode.None), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(keyButtonText);
            UIFactory.SetLocalizedText(keyButtonText, () =>
                _isWaitingForAnimatorParamsKeyInput
                    ? Localization.PressAnyKey
                    : GetKeyCodeDisplayName(UIConfig?.AnimatorParamsToggleKey ?? KeyCode.None));
        }

        private void BuildYsmRadialMenuKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);
            var label = UIFactory.CreateText("YsmWheelKeyLabel", row.transform,
                Localization.YsmRadialMenuHotkey, 18, Color.white);
            SetupSettingLabel(label, 140);
            UIFactory.SetLocalizedText(label, () => Localization.YsmRadialMenuHotkey);
            var button = UIFactory.CreateButton("YsmWheelKeyButton", row.transform, () =>
            {
                if (UIConfig == null) return;
                _isWaitingForUIKeyInput = _isWaitingForAnimatorParamsKeyInput = false;
                _isWaitingForModifierKey1Input = _isWaitingForModifierKey2Input = false;
                _isWaitingForYsmRadialMenuKeyInput = true;
                RefreshAllKeyButtons();
            }, new(0.2f, 0.2f, 0.2f, 1));
            _ysmRadialMenuKeyButton = button;
            SetupSettingControl(button, new(100, 30));
            var text = UIFactory.CreateText("Text", button.transform,
                GetKeyCodeDisplayName(UIConfig?.YsmRadialMenuKey ?? KeyCode.Z), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(text);
            UIFactory.SetLocalizedText(text, () => _isWaitingForYsmRadialMenuKeyInput
                ? Localization.PressAnyKey
                : GetKeyCodeDisplayName(UIConfig?.YsmRadialMenuKey ?? KeyCode.Z));
        }

        private void BuildModifierKeySettings(GameObject parent)
        {
            var modifier1Row = CreateSettingRow(parent);
            var modifier1Label = UIFactory.CreateText("ModifierKey1Label", modifier1Row.transform,
                Localization.EmotionModifierKey1, 18, Color.white);
            SetupSettingLabel(modifier1Label, 140);
            UIFactory.SetLocalizedText(modifier1Label, () => Localization.EmotionModifierKey1);

            var modifier1Button = UIFactory.CreateButton("ModifierKey1Button", modifier1Row.transform,
                OnModifierKey1ButtonClicked, new(0.2f, 0.2f, 0.2f, 1));
            _modifierKey1Button = modifier1Button;
            SetupSettingControl(modifier1Button, new(100, 30));

            var modifier1ButtonText = UIFactory.CreateText("Text", modifier1Button.transform,
                GetKeyCodeDisplayName(UIConfig?.EmotionModifierKey1 ?? KeyCode.Comma), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(modifier1ButtonText);
            UIFactory.SetLocalizedText(modifier1ButtonText, () =>
                _isWaitingForModifierKey1Input
                    ? Localization.PressAnyKey
                    : GetKeyCodeDisplayName(UIConfig?.EmotionModifierKey1 ?? KeyCode.Comma));

            var modifier2Row = CreateSettingRow(parent);
            var modifier2Label = UIFactory.CreateText("ModifierKey2Label", modifier2Row.transform,
                Localization.EmotionModifierKey2, 18, Color.white);
            SetupSettingLabel(modifier2Label, 140);
            UIFactory.SetLocalizedText(modifier2Label, () => Localization.EmotionModifierKey2);

            var modifier2Button = UIFactory.CreateButton("ModifierKey2Button", modifier2Row.transform,
                OnModifierKey2ButtonClicked, new(0.2f, 0.2f, 0.2f, 1));
            _modifierKey2Button = modifier2Button;
            SetupSettingControl(modifier2Button, new(100, 30));

            var modifier2ButtonText = UIFactory.CreateText("Text", modifier2Button.transform,
                GetKeyCodeDisplayName(UIConfig?.EmotionModifierKey2 ?? KeyCode.Period), 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(modifier2ButtonText);
            UIFactory.SetLocalizedText(modifier2ButtonText, () =>
                _isWaitingForModifierKey2Input
                    ? Localization.PressAnyKey
                    : GetKeyCodeDisplayName(UIConfig?.EmotionModifierKey2 ?? KeyCode.Period));

            var warningRow = new GameObject("WarningRow", typeof(RectTransform), typeof(Image));
            warningRow.transform.SetParent(parent.transform, false);

            var rowImage = warningRow.GetComponent<Image>();
            rowImage.color = new(0.3f, 0.2f, 0.05f, 0.9f);

            rowImage.raycastTarget = false;

            UIFactory.SetupAnchor(warningRow, new(0, 1), new(1, 1), new(0.5f, 1), new(0, 0), Vector2.zero);

            var warningRowLayoutElement = warningRow.AddComponent<LayoutElement>();
            warningRowLayoutElement.minWidth = 0;
            warningRowLayoutElement.preferredWidth = 0;
            warningRowLayoutElement.flexibleWidth = 1;
            warningRowLayoutElement.flexibleHeight = 1;

            UIFactory.SetupVerticalLayoutGroup(warningRow, 0f, new(20, 20, 10, 10),
                TextAnchor.UpperLeft, true, true, true);

            var warningText = UIFactory.CreateText("WarningText", warningRow.transform,
                Localization.EmotionModifierKeyWarning, 16, new Color(1f, 0.8f, 0.4f, 1f), TextAnchor.UpperLeft);

            UIFactory.SetupRectTransform(warningText, Vector2.zero, Vector2.one, Vector2.zero);

            var warningTextComponent = warningText.GetComponent<TextMeshProUGUI>();
            if (warningTextComponent != null)
                warningTextComponent.enableWordWrapping = true;

            UIFactory.SetLocalizedText(warningText, () => Localization.EmotionModifierKeyWarning);

            var warningLayoutElement = warningText.AddComponent<LayoutElement>();
            warningLayoutElement.flexibleWidth = 1;
            warningLayoutElement.flexibleHeight = 1;

            UIFactory.SetupContentSizeFitter(warningText, ContentSizeFitter.FitMode.Unconstrained);
            UIFactory.SetupContentSizeFitter(warningRow, ContentSizeFitter.FitMode.Unconstrained);
        }

        private void BuildAnimatorParamsToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowAnimatorParameters, 18, Color.white);
            SetupSettingLabel(label, 60);
            UIFactory.SetLocalizedText(label, () => Localization.ShowAnimatorParameters);

            var configWindow = GetComponentInParent<ConfigWindow>();
            var toggle = UIFactory.CreateToggle("AnimatorParamsToggle", row.transform,
                configWindow?.IsAnimatorParamsWindowVisible() ?? false, SetAnimatorParamsWindowVisible, true);
            SetupSettingControl(toggle.gameObject, new(20, 20));

            _animatorParamsToggle = toggle;
        }

        private void BuildShowDCMButtonToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowDCMButton, 18, Color.white);
            SetupSettingLabel(label, 60);
            UIFactory.SetLocalizedText(label, () => Localization.ShowDCMButton);

            var toggle = UIFactory.CreateToggle("ShowDCMButtonToggle", row.transform,
                UIConfig?.ShowDCMButton ?? true, OnShowDCMButtonToggleChanged, true);
            SetupSettingControl(toggle.gameObject, new(20, 20));
        }

        private static void OnShowDCMButtonToggleChanged(bool value)
        {
            if (UIConfig == null) return;
            UIConfig.ShowDCMButton = value;
            ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
        }

        private void BuildDCMButtonPositionSettings(GameObject parent)
        {
            var anchorRow = CreateSettingRow(parent);
            var anchorLabel = UIFactory.CreateText("AnchorLabel", anchorRow.transform,
                Localization.DCMButtonAnchor, 18, Color.white);
            SetupSettingLabel(anchorLabel, 192);
            UIFactory.SetLocalizedText(anchorLabel, () => Localization.DCMButtonAnchor);

            var dropdown = UIFactory.CreateDropdown("AnchorDropdown", anchorRow.transform, OnAnchorDropdownChanged);
            SetupSettingControl(dropdown.gameObject, new(160, 30));

            RefreshAnchorDropdownOptions(dropdown);

            _dcmButtonAnchorDropdown = dropdown;

            var dropdownLocalizedText = dropdown.gameObject.AddComponent<LocalizedDropdown>();
            dropdownLocalizedText.SetDropdown(dropdown);
            dropdownLocalizedText.SetRefreshAction(RefreshAnchorDropdownOnLanguageChange);

            var offsetRow = CreateSettingRow(parent);
            var offsetLabel = UIFactory.CreateText("OffsetLabel", offsetRow.transform,
                Localization.DCMButtonOffset, 18, Color.white);

            UIFactory.SetLocalizedText(offsetLabel, () => Localization.DCMButtonOffset);

            var offsetYInput = UIFactory.CreateInputField("OffsetYInput", offsetRow.transform);
            UIFactory.SetupRectTransform(offsetYInput.gameObject, new(.5f, 0), new(1, 0),
                offsetMin: new(40, 10), offsetMax: new(-12, 35));
            offsetYInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            offsetYInput.onValueChanged.AddListener(OnOffsetYValueChanged);
            offsetYInput.onEndEdit.AddListener(OnOffsetYEndEdit);
            _dcmButtonOffsetYInput = offsetYInput;

            var offsetYLabel = UIFactory.CreateText("OffsetYLabel", offsetRow.transform,
                Localization.OffsetY, 18, new Color(0.9f, 0.9f, 0.9f, 1));
            UIFactory.SetupRectTransform(offsetYLabel, new(.5f, 0), new(.5f, 0),
                offsetMin: new(8, 10), offsetMax: new(36, 35));
            UIFactory.SetLocalizedText(offsetYLabel, () => Localization.OffsetY);

            var offsetXInput = UIFactory.CreateInputField("OffsetXInput", offsetRow.transform);
            UIFactory.SetupRectTransform(offsetXInput.gameObject, Vector2.zero, new(.5f, 0),
                offsetMin: new(44, 10), offsetMax: new(-8, 35));
            offsetXInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            offsetXInput.onValueChanged.AddListener(OnOffsetXValueChanged);
            offsetXInput.onEndEdit.AddListener(OnOffsetXEndEdit);
            _dcmButtonOffsetXInput = offsetXInput;

            var offsetXLabel = UIFactory.CreateText("OffsetXLabel", offsetRow.transform,
                Localization.OffsetX, 18, new Color(0.9f, 0.9f, 0.9f, 1));
            UIFactory.SetupRectTransform(offsetXLabel, Vector2.zero, Vector2.zero,
                offsetMin: new(12, 10), offsetMax: new(40, 35));
            UIFactory.SetLocalizedText(offsetXLabel, () => Localization.OffsetX);
            offsetRow.AddComponent<ButtonOffsetRowLayout>().Initialize(offsetLabel, offsetXLabel,
                offsetXInput.gameObject, offsetYLabel, offsetYInput.gameObject);

            RefreshDCMButtonPositionDisplay();
        }

        private static void RefreshAnchorDropdownOptions(TMP_Dropdown dropdown)
        {
            dropdown.options.Clear();
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.TopLeft)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.TopCenter)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.TopRight)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.MiddleLeft)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.MiddleCenter)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.MiddleRight)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.BottomLeft)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.BottomCenter)));
            dropdown.options.Add(new(GetAnchorPositionText(AnchorPosition.BottomRight)));
        }

        private void RefreshAnchorDropdownOnLanguageChange()
        {
            if (_dcmButtonAnchorDropdown == null) return;
            var currentValue = _dcmButtonAnchorDropdown.value;
            RefreshAnchorDropdownOptions(_dcmButtonAnchorDropdown);
            if (currentValue < 0 || currentValue >= _dcmButtonAnchorDropdown.options.Count) return;
            _dcmButtonAnchorDropdown.value = -1;
            _dcmButtonAnchorDropdown.value = currentValue;
        }

        private void RefreshDCMButtonPositionDisplay()
        {
            if (UIConfig == null) return;

            if (_dcmButtonAnchorDropdown != null)
            {
                var anchorValues = Enum.GetValues(typeof(AnchorPosition));
                var index = Array.IndexOf(anchorValues, UIConfig.DCMButtonAnchor);
                if (index >= 0 && index < _dcmButtonAnchorDropdown.options.Count)
                    _dcmButtonAnchorDropdown.value = index;
            }

            if (_dcmButtonOffsetXInput != null)
                _dcmButtonOffsetXInput.text = UIConfig.DCMButtonOffsetX.ToString("F1");

            if (_dcmButtonOffsetYInput != null)
                _dcmButtonOffsetYInput.text = UIConfig.DCMButtonOffsetY.ToString("F1");
        }

        private static string GetAnchorPositionText(AnchorPosition position)
        {
            return position switch
            {
                AnchorPosition.TopLeft => Localization.TopLeft,
                AnchorPosition.TopCenter => Localization.TopCenter,
                AnchorPosition.TopRight => Localization.TopRight,
                AnchorPosition.MiddleLeft => Localization.MiddleLeft,
                AnchorPosition.MiddleCenter => Localization.MiddleCenter,
                AnchorPosition.MiddleRight => Localization.MiddleRight,
                AnchorPosition.BottomLeft => Localization.BottomLeft,
                AnchorPosition.BottomCenter => Localization.BottomCenter,
                AnchorPosition.BottomRight => Localization.BottomRight,
                _ => Localization.TopLeft,
            };
        }

        private void OnAnchorDropdownChanged(int index)
        {
            if (UIConfig == null) return;
            var anchorValues = Enum.GetValues(typeof(AnchorPosition));
            if (index < 0 || index >= anchorValues.Length) return;
            UIConfig.DCMButtonAnchor = (AnchorPosition)anchorValues.GetValue(index);
            ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
            RefreshSettingsButton();
        }

        private void OnOffsetXValueChanged(string value)
        {
            if (UIConfig == null) return;
            if (!float.TryParse(value, out var offsetX)) return;
            UIConfig.DCMButtonOffsetX = offsetX;
            RefreshSettingsButton();
        }

        private void OnOffsetXEndEdit(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetX))
            {
                UIConfig.DCMButtonOffsetX = offsetX;
                ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                RefreshSettingsButton();
            }
            else
            {
                RefreshDCMButtonPositionDisplay();
            }
        }

        private void OnOffsetYValueChanged(string value)
        {
            if (UIConfig == null) return;
            if (!float.TryParse(value, out var offsetY)) return;
            UIConfig.DCMButtonOffsetY = offsetY;
            RefreshSettingsButton();
        }

        private void OnOffsetYEndEdit(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetY))
            {
                UIConfig.DCMButtonOffsetY = offsetY;
                ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                RefreshSettingsButton();
            }
            else
            {
                RefreshDCMButtonPositionDisplay();
            }
        }

        private void RefreshSettingsButton()
        {
            var configWindow = GetComponentInParent<ConfigWindow>();
            configWindow?.RefreshSettingsButton();
        }

        private static void BuildSectionHeading(GameObject parent, Func<string> text)
        {
            var heading = UIFactory.CreateText("SectionHeading", parent.transform, text(), 14, UITheme.Text);
            var layout = heading.AddComponent<LayoutElement>();
            layout.minWidth = 0;
            layout.preferredWidth = 0;
            layout.preferredHeight = 28;
            layout.flexibleWidth = 1;
            UIFactory.SetLocalizedText(heading, text);
        }

        private static void SetupSettingLabel(GameObject label, float reservedRight)
        {
            UIFactory.SetupBoundedLeftLabel(label, reservedRight);
        }

        private static void SetupSettingControl(GameObject control, Vector2 size, float rightOffset = -20)
        {
            UIFactory.SetupRightControl(control, size, rightOffset);
        }

        private GameObject CreateSettingRow(GameObject parent)
        {
            var row = new GameObject("SettingRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent.transform, false);

            var rowImage = row.GetComponent<Image>();
            rowImage.color = UITheme.Surface;
            rowImage.raycastTarget = false;

            UIFactory.SetupAnchor(row, new(0, 1), new(1, 1), new(0.5f, 1), new(0, 50), Vector2.zero);

            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 44;
            rowLayoutElement.minWidth = 0;
            rowLayoutElement.preferredWidth = 0;
            rowLayoutElement.flexibleWidth = 1;
            rowLayoutElement.flexibleHeight = 0;

            return row;
        }

        private void OnKeyButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForYsmRadialMenuKeyInput = false;
            _isWaitingForUIKeyInput = true;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey1Input = false;
            _isWaitingForModifierKey2Input = false;
            RefreshAllKeyButtons();
        }

        private void OnAnimatorParamsKeyButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForYsmRadialMenuKeyInput = false;
            _isWaitingForAnimatorParamsKeyInput = true;
            _isWaitingForUIKeyInput = false;
            _isWaitingForModifierKey1Input = false;
            _isWaitingForModifierKey2Input = false;
            RefreshAllKeyButtons();
        }

        private void OnAnimatorParamsClearButtonClicked()
        {
            if (UIConfig == null) return;
            UIConfig.AnimatorParamsToggleKey = KeyCode.None;
            ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
            RefreshKeyButton(_animatorParamsKeyButton);
        }

        private void OnModifierKey1ButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForYsmRadialMenuKeyInput = false;
            _isWaitingForModifierKey1Input = true;
            _isWaitingForUIKeyInput = false;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey2Input = false;
            RefreshAllKeyButtons();
        }

        private void OnModifierKey2ButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForYsmRadialMenuKeyInput = false;
            _isWaitingForModifierKey2Input = true;
            _isWaitingForUIKeyInput = false;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey1Input = false;
            RefreshAllKeyButtons();
        }

        private void HandleKeyInputCapture()
        {
            if (!IsWaitingForKeyInput || UIConfig == null) return;

            if (InputBlocker.GetRealKeyDown(KeyCode.Escape))
            {
                _isWaitingForUIKeyInput = false;
                _isWaitingForAnimatorParamsKeyInput = false;
                _isWaitingForModifierKey1Input = false;
                _isWaitingForModifierKey2Input = false;
                _isWaitingForYsmRadialMenuKeyInput = false;
                RefreshAllKeyButtons();
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (InputBlocker.GetRealKeyDown(keyCode))
                {
                    if (keyCode is KeyCode.Mouse0 or KeyCode.Mouse1 or KeyCode.Mouse2 or KeyCode.Mouse3
                        or KeyCode.Mouse4 or KeyCode.Mouse5 or KeyCode.Mouse6)
                        continue;

                    if (_isWaitingForYsmRadialMenuKeyInput)
                    {
                        UIConfig.YsmRadialMenuKey = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForYsmRadialMenuKeyInput = false;
                        RefreshKeyButton(_ysmRadialMenuKeyButton);
                    }
                    else if (_isWaitingForUIKeyInput)
                    {
                        UIConfig.ToggleKey = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForUIKeyInput = false;
                        RefreshKeyButton(_keyButton);
                    }
                    else if (_isWaitingForAnimatorParamsKeyInput)
                    {
                        UIConfig.AnimatorParamsToggleKey = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForAnimatorParamsKeyInput = false;
                        RefreshKeyButton(_animatorParamsKeyButton);
                    }
                    else if (_isWaitingForModifierKey1Input)
                    {
                        UIConfig.EmotionModifierKey1 = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForModifierKey1Input = false;
                        RefreshKeyButton(_modifierKey1Button);
                    }
                    else if (_isWaitingForModifierKey2Input)
                    {
                        UIConfig.EmotionModifierKey2 = keyCode;
                        ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                        _isWaitingForModifierKey2Input = false;
                        RefreshKeyButton(_modifierKey2Button);
                    }

                    return;
                }
        }

        private void RefreshAllKeyButtons()
        {
            RefreshKeyButton(_keyButton);
            RefreshKeyButton(_animatorParamsKeyButton);
            RefreshKeyButton(_ysmRadialMenuKeyButton);
            RefreshKeyButton(_modifierKey1Button);
            RefreshKeyButton(_modifierKey2Button);
        }

        private static void RefreshKeyButton(GameObject? button)
        {
            if (button == null) return;
            var textObj = button.transform.Find("Text");
            if (textObj == null) return;
            var localizedText = textObj.GetComponent<LocalizedText>();
            localizedText?.RefreshText();
        }

        private static string GetKeyCodeDisplayName(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None) return Localization.None;
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

        private void BuildUpdateCheckSection(GameObject parent)
        {
            var updatePanel = parent;
            var checkButton = UIFactory.CreateButton("UpdateCheckButton", updatePanel.transform,
                OnUpdateCheckButtonClicked, UITheme.Accent);
            var checkLayout = checkButton.AddComponent<LayoutElement>();
            checkLayout.minWidth = 0;
            checkLayout.minHeight = 40;
            checkLayout.preferredHeight = 40;
            checkLayout.flexibleHeight = 0;
            checkLayout.flexibleWidth = 1;
            var checkText = UIFactory.CreateText("Text", checkButton.transform, "", 16,
                Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(checkText);
            _updateCheckButtonText = checkText.AddComponent<LocalizedText>();
            _updateCheckButtonText.SetTextGetter(GetCheckButtonText);

            var updateInfoText = UIFactory.CreateText("UpdateInfo", updatePanel.transform, "", 16,
                UITheme.Text, TextAnchor.UpperLeft);
            var infoText = updateInfoText.GetComponent<TextMeshProUGUI>();
            infoText.enableWordWrapping = true;
            infoText.overflowMode = TextOverflowModes.Masking;
            var infoLayout = updateInfoText.AddComponent<LayoutElement>();
            infoLayout.minWidth = 0;
            infoLayout.preferredWidth = 0;
            infoLayout.flexibleWidth = 1;
            infoLayout.flexibleHeight = 0;
            _updateInfoLocalizedText = updateInfoText.AddComponent<LocalizedText>();
            _updateInfoLocalizedText.SetTextGetter(GetUpdateInfoText);

            var logScroll = UIFactory.CreateScrollView("ChangelogScrollView", updatePanel.transform,
                out var logContent);
            var logLayout = logScroll.gameObject.AddComponent<LayoutElement>();
            logLayout.minWidth = 0;
            logLayout.minHeight = 0;
            logLayout.preferredWidth = 0;
            logLayout.preferredHeight = 0;
            logLayout.flexibleWidth = 1;
            logLayout.flexibleHeight = 1;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(logScroll.transform, false);
            UIFactory.SetupRectTransform(viewport, Vector2.zero, Vector2.one, Vector2.zero,
                new(10, 0), Vector2.zero);
            logScroll.viewport = (RectTransform)viewport.transform;
            logContent.transform.SetParent(viewport.transform, false);
            UIFactory.SetupRectTransform(logContent, new(0, 1), Vector2.one, Vector2.zero,
                pivot: new(0, 1), anchoredPosition: Vector2.zero);
            UIFactory.SetupVerticalLayoutGroup(logContent, 0, new(8, 8, 8, 8),
                TextAnchor.UpperLeft, true, true, true);
            UIFactory.SetupContentSizeFitter(logContent, ContentSizeFitter.FitMode.Unconstrained);
            var logScrollbar = UIFactory.CreateScrollbar(logScroll, 6f, false);
            logScrollbar.transform.SetParent(logScroll.transform, false);

            _changelogText = UIFactory.CreateText("ChangelogText", logContent.transform, "", 14,
                UITheme.Text, TextAnchor.UpperLeft);
            var changelogBody = _changelogText.GetComponent<TextMeshProUGUI>();
            changelogBody.enableWordWrapping = true;
            changelogBody.enableAutoSizing = false;
            changelogBody.overflowMode = TextOverflowModes.Masking;
            changelogBody.maskable = true;
            var changelogLayout = _changelogText.AddComponent<LayoutElement>();
            changelogLayout.minWidth = 0;
            changelogLayout.preferredWidth = 0;
            changelogLayout.flexibleWidth = 1;
            _changelogText.AddComponent<LinkHandler>();

            var downloadSection = new GameObject("DownloadSection", typeof(RectTransform));
            downloadSection.transform.SetParent(updatePanel.transform, false);
            downloadSection.SetActive(false);

            UIFactory.SetupVerticalLayoutGroup(downloadSection, 6f, new(0, 0, 0, 0),
                childControlHeight: true, childControlWidth: true, childForceExpandHeight: false,
                childForceExpandWidth: true);

            var downloadButtonsContainer = new GameObject("DownloadButtonsContainer", typeof(RectTransform));
            downloadButtonsContainer.transform.SetParent(downloadSection.transform, false);

            downloadButtonsContainer.AddComponent<DownloadButtonsLayout>();
            var downloadLayout = downloadSection.AddComponent<LayoutElement>();
            downloadLayout.minWidth = 0;
            downloadLayout.preferredWidth = 0;
            downloadLayout.flexibleWidth = 1;
            downloadLayout.flexibleHeight = 0;

            _downloadButtonsContainer = downloadSection;

            UpdateChecker.OnUpdateCheckCompleted += OnUpdateCheckCompleted;

            _lastUpdateInfoRefreshTime = Time.unscaledTime;
            RefreshUpdateInfo();
        }

        private static void OnUpdateCheckButtonClicked()
        {
            if (UpdateChecker.Instance != null) UpdateChecker.Instance.CheckForUpdate();
        }

        private void OnUpdateCheckCompleted(bool hasUpdate, string? latestVersion)
        {
            RefreshUpdateInfo();
        }

        private static void OpenURL(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            Application.OpenURL(url);
        }

        private static string GetUpdateInfoText()
        {
            var updateChecker = UpdateChecker.Instance;
            if (updateChecker == null)
                return Localization.UpdateCheckNotAvailable;

            var hasUpdate = updateChecker.HasUpdate();
            var latestVersion = updateChecker.GetLatestVersion();
            var latestReleaseName = updateChecker.GetLatestReleaseName();
            var latestPublishedAt = updateChecker.GetLatestPublishedAt();

            var info = new StringBuilder();

            if (hasUpdate && !string.IsNullOrEmpty(latestVersion))
            {
                var displayVersion = !string.IsNullOrEmpty(latestReleaseName) ? latestReleaseName : $"v{latestVersion}";
                info.AppendLine($"{Localization.UpdateAvailable}: {displayVersion}");
            }
            else if (!string.IsNullOrEmpty(latestVersion))
            {
                var displayVersion = !string.IsNullOrEmpty(latestReleaseName) ? latestReleaseName : $"v{latestVersion}";
                info.AppendLine($"{Localization.LatestVersion}: {displayVersion}");
            }

            if (!latestPublishedAt.HasValue) return info.ToString();
            var localTime = latestPublishedAt.Value.ToLocalTime();
            info.AppendLine($"{Localization.PublishedAt}: {localTime:yyyy-MM-dd HH:mm}");

            return info.ToString();
        }

        private static string GetCheckButtonText()
        {
            var updateChecker = UpdateChecker.Instance;
            if (updateChecker == null)
                return Localization.CheckForUpdate;

            var lastCheckTime = updateChecker.GetLastCheckTime();
            var buttonText = Localization.CheckForUpdate;

            if (!lastCheckTime.HasValue) return buttonText;
            var timeAgo = DateTime.Now - lastCheckTime.Value;
            var timeText = timeAgo switch
            {
                { TotalMinutes: < 1 } => Localization.JustNow,
                { TotalHours: < 1 } => $"{(int)timeAgo.TotalMinutes} {Localization.MinutesAgo}",
                { TotalDays: < 1 } => $"{(int)timeAgo.TotalHours} {Localization.HoursAgo}",
                _ => $"{(int)timeAgo.TotalDays} {Localization.DaysAgo}",
            };

            buttonText = $"{Localization.CheckForUpdate} ({Localization.LastCheckTime}: {timeText})";

            return buttonText;
        }

        private void RefreshUpdateInfo()
        {
            if (_updateInfoLocalizedText == null) return;
            _updateInfoLocalizedText.RefreshText();

            if (_updateCheckButtonText != null)
                _updateCheckButtonText.RefreshText();

            var updateChecker = UpdateChecker.Instance;
            if (updateChecker == null) return;

            var changelog = updateChecker.GetLatestChangelog();
            var downloadLinks = updateChecker.GetLatestDownloadLinks();

            if (_changelogText != null && _displayedChangelog != changelog)
            {
                _displayedChangelog = changelog;
                _changelogText.SetActive(!string.IsNullOrEmpty(changelog));
                var text = _changelogText.GetComponent<TextMeshProUGUI>();
                text.text = string.IsNullOrEmpty(changelog)
                    ? string.Empty
                    : MarkdownToRichTextConverter.Convert(changelog!, (int)text.fontSize, 20);
            }

            var signature = downloadLinks == null
                ? string.Empty
                : string.Join("\n", downloadLinks.Select(link => $"{link.Name}\t{link.Url}"));
            if (_downloadSignature == signature) return;
            _downloadSignature = signature;

            if (_downloadButtonsContainer == null) return;

            var buttonsContainer = _downloadButtonsContainer.transform.Find("DownloadButtonsContainer");
            if (buttonsContainer != null)
                foreach (Transform child in buttonsContainer)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }

            var hasDownloadLinks = downloadLinks is { Count: > 0 };
            _downloadButtonsContainer.SetActive(hasDownloadLinks);

            if (downloadLinks is not { Count: > 0 } || buttonsContainer == null) return;
            foreach (var link in downloadLinks)
            {
                var button = UIFactory.CreateButton("DownloadLink", buttonsContainer,
                    () => OpenURL(link.Url), UITheme.Accent);
                var label = UIFactory.CreateText("Text", button.transform, link.Name, 14,
                    UITheme.Text, TextAnchor.MiddleCenter);
                UIFactory.SetupButtonText(label);
            }
        }
    }

    internal sealed class DownloadButtonsLayout : LayoutGroup
    {
        private const float Spacing = 6;
        private const float RowHeight = 34;

        private int Columns => Mathf.Max(1, Mathf.FloorToInt((rectTransform.rect.width + Spacing) / 146));

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(0, 0, 1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            var rows = Mathf.CeilToInt((float)rectChildren.Count / Columns);
            var height = rows * RowHeight + Mathf.Max(0, rows - 1) * Spacing;
            SetLayoutInputForAxis(height, height, 0, 1);
        }

        public override void SetLayoutHorizontal()
        {
            var columns = Columns;
            var width = Mathf.Min(200, Mathf.Max(0, (rectTransform.rect.width - (columns - 1) * Spacing) / columns));
            for (var i = 0; i < rectChildren.Count; i++)
            {
                var rowItems = Mathf.Min(columns, rectChildren.Count - i / columns * columns);
                var left = (rectTransform.rect.width - rowItems * width - (rowItems - 1) * Spacing) * .5f;
                SetChildAlongAxis(rectChildren[i], 0, left + i % columns * (width + Spacing), width);
            }
        }

        public override void SetLayoutVertical()
        {
            var columns = Columns;
            for (var i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 1, i / columns * (RowHeight + Spacing), RowHeight);
        }
    }
}
