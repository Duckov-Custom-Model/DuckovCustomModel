using System;
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
        private GameObject? _downloadButtonsContainer;
        private bool _isWaitingForAnimatorParamsKeyInput;
        private bool _isWaitingForModifierKey1Input;
        private bool _isWaitingForModifierKey2Input;

        private bool _isWaitingForUIKeyInput;
        private GameObject? _keyButton;
        private float _lastUpdateInfoRefreshTime;
        private GameObject? _modifierKey1Button;
        private GameObject? _modifierKey2Button;

        private int _settingRowIndex;
        private Toggle? _showDCMButtonToggle;
        private GameObject? _updateCheckButton;
        private LocalizedText? _updateCheckButtonText;
        private LocalizedText? _updateInfoLocalizedText;
        private GameObject? _updateInfoPanel;

        private static UIConfig? UIConfig => ModEntry.UIConfig;

        public bool IsWaitingForKeyInput => _isWaitingForUIKeyInput || _isWaitingForAnimatorParamsKeyInput ||
                                            _isWaitingForModifierKey1Input || _isWaitingForModifierKey2Input;

        private void Update()
        {
            if (_isWaitingForUIKeyInput || _isWaitingForAnimatorParamsKeyInput ||
                _isWaitingForModifierKey1Input || _isWaitingForModifierKey2Input)
                HandleKeyInputCapture();

            if (_updateInfoLocalizedText == null || !(Time.time - _lastUpdateInfoRefreshTime > 30f)) return;
            _lastUpdateInfoRefreshTime = Time.time;
            RefreshUpdateInfo();
        }

        protected override void OnDestroy()
        {
            UpdateChecker.OnUpdateCheckCompleted -= OnUpdateCheckCompleted;
            base.OnDestroy();
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

            var outline = PanelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);
        }

        protected override void BuildContent()
        {
            if (PanelRoot == null) return;

            _settingRowIndex = 0;
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

            UIFactory.SetupVerticalLayoutGroup(contentArea, 10f, new(10, 10, 10, 10));
            UIFactory.SetupContentSizeFitter(contentArea, ContentSizeFitter.FitMode.PreferredSize,
                ContentSizeFitter.FitMode.Unconstrained);

            BuildKeySetting(contentArea);
            BuildAnimatorParamsKeySetting(contentArea);
            BuildModifierKeySettings(contentArea);
            BuildAnimatorParamsToggle(contentArea);
            BuildShowDCMButtonToggle(contentArea);
            BuildDCMButtonPositionSettings(contentArea);
            BuildUpdateCheckSection(contentArea);

            if (contentArea != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea.GetComponent<RectTransform>());
        }

        private void BuildKeySetting(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var keyLabel = UIFactory.CreateText("KeyLabel", row.transform,
                Localization.Hotkey, 18, Color.white);
            UIFactory.SetupLeftLabel(keyLabel);
            UIFactory.SetupContentSizeFitter(keyLabel);
            UIFactory.SetLocalizedText(keyLabel, () => Localization.Hotkey);

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            _keyButton = keyButton;
            UIFactory.SetupRightControl(keyButton, new(100, 30));

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
            UIFactory.SetupLeftLabel(keyLabel);
            UIFactory.SetupContentSizeFitter(keyLabel);
            UIFactory.SetLocalizedText(keyLabel, () => Localization.AnimatorParamsHotkey);

            var keyButton = UIFactory.CreateButton("KeyButton", row.transform, OnAnimatorParamsKeyButtonClicked,
                new(0.2f, 0.2f, 0.2f, 1));
            _animatorParamsKeyButton = keyButton;
            UIFactory.SetupRightControl(keyButton, new(100, 30));

            var clearButton = UIFactory.CreateButton("ClearButton", row.transform, OnAnimatorParamsClearButtonClicked,
                new(0.3f, 0.2f, 0.2f, 1));
            UIFactory.SetupRightControl(clearButton, new(60, 30), -20f - 100f - 8f);

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

        private void BuildModifierKeySettings(GameObject parent)
        {
            var modifier1Row = CreateSettingRow(parent);
            var modifier1Label = UIFactory.CreateText("ModifierKey1Label", modifier1Row.transform,
                Localization.EmotionModifierKey1, 18, Color.white);
            UIFactory.SetupLeftLabel(modifier1Label);
            UIFactory.SetupContentSizeFitter(modifier1Label);
            UIFactory.SetLocalizedText(modifier1Label, () => Localization.EmotionModifierKey1);

            var modifier1Button = UIFactory.CreateButton("ModifierKey1Button", modifier1Row.transform,
                OnModifierKey1ButtonClicked, new(0.2f, 0.2f, 0.2f, 1));
            _modifierKey1Button = modifier1Button;
            UIFactory.SetupRightControl(modifier1Button, new(100, 30));

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
            UIFactory.SetupLeftLabel(modifier2Label);
            UIFactory.SetupContentSizeFitter(modifier2Label);
            UIFactory.SetLocalizedText(modifier2Label, () => Localization.EmotionModifierKey2);

            var modifier2Button = UIFactory.CreateButton("ModifierKey2Button", modifier2Row.transform,
                OnModifierKey2ButtonClicked, new(0.2f, 0.2f, 0.2f, 1));
            _modifierKey2Button = modifier2Button;
            UIFactory.SetupRightControl(modifier2Button, new(100, 30));

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
            rowImage.color = new Color(0.3f, 0.2f, 0.05f, 0.9f);

            var outline = warningRow.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.6f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            UIFactory.SetupAnchor(warningRow, new(0, 1), new(1, 1), new(0.5f, 1), new(800, 0), Vector2.zero);

            var warningRowLayoutElement = warningRow.AddComponent<LayoutElement>();
            warningRowLayoutElement.minWidth = 800;
            warningRowLayoutElement.preferredWidth = 800;
            warningRowLayoutElement.flexibleWidth = 0;
            warningRowLayoutElement.flexibleHeight = 1;

            UIFactory.SetupVerticalLayoutGroup(warningRow, 0f, new RectOffset(20, 20, 10, 10),
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
            UIFactory.SetupContentSizeFitter(warningRow, ContentSizeFitter.FitMode.PreferredSize,
                ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildAnimatorParamsToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowAnimatorParameters, 18, Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);
            UIFactory.SetLocalizedText(label, () => Localization.ShowAnimatorParameters);

            var configWindow = GetComponentInParent<ConfigWindow>();
            var toggle = UIFactory.CreateToggle("AnimatorParamsToggle", row.transform,
                configWindow?.IsAnimatorParamsWindowVisible() ?? false, SetAnimatorParamsWindowVisible);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

            _animatorParamsToggle = toggle;
        }

        private void BuildShowDCMButtonToggle(GameObject parent)
        {
            var row = CreateSettingRow(parent);

            var label = UIFactory.CreateText("Label", row.transform,
                Localization.ShowDCMButton, 18, Color.white);
            UIFactory.SetupLeftLabel(label);
            UIFactory.SetupContentSizeFitter(label);
            UIFactory.SetLocalizedText(label, () => Localization.ShowDCMButton);

            var toggle = UIFactory.CreateToggle("ShowDCMButtonToggle", row.transform,
                UIConfig?.ShowDCMButton ?? true, OnShowDCMButtonToggleChanged);
            UIFactory.SetupRightControl(toggle.gameObject, new(20, 20));

            _showDCMButtonToggle = toggle;
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
            UIFactory.SetupLeftLabel(anchorLabel);
            UIFactory.SetupContentSizeFitter(anchorLabel);
            UIFactory.SetLocalizedText(anchorLabel, () => Localization.DCMButtonAnchor);

            var dropdown = UIFactory.CreateDropdown("AnchorDropdown", anchorRow.transform, OnAnchorDropdownChanged);
            UIFactory.SetupRightControl(dropdown.gameObject, new(200, 30));

            RefreshAnchorDropdownOptions(dropdown);

            _dcmButtonAnchorDropdown = dropdown;

            var dropdownLocalizedText = dropdown.gameObject.AddComponent<LocalizedDropdown>();
            dropdownLocalizedText.SetDropdown(dropdown);
            dropdownLocalizedText.SetRefreshAction(RefreshAnchorDropdownOnLanguageChange);

            var offsetRow = CreateSettingRow(parent);
            var offsetLabel = UIFactory.CreateText("OffsetLabel", offsetRow.transform,
                Localization.DCMButtonOffset, 18, Color.white);
            UIFactory.SetupLeftLabel(offsetLabel);
            UIFactory.SetupContentSizeFitter(offsetLabel);
            UIFactory.SetLocalizedText(offsetLabel, () => Localization.DCMButtonOffset);

            var offsetYInput = UIFactory.CreateInputField("OffsetYInput", offsetRow.transform);
            UIFactory.SetupRightControl(offsetYInput.gameObject, new(80, 25));
            offsetYInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            offsetYInput.onValueChanged.AddListener(OnOffsetYValueChanged);
            offsetYInput.onEndEdit.AddListener(OnOffsetYEndEdit);
            _dcmButtonOffsetYInput = offsetYInput;

            var offsetYLabel = UIFactory.CreateText("OffsetYLabel", offsetRow.transform,
                Localization.OffsetY, 16, new Color(0.9f, 0.9f, 0.9f, 1));
            UIFactory.SetupRightLabel(offsetYLabel);
            UIFactory.SetupContentSizeFitter(offsetYLabel);
            UIFactory.SetLocalizedText(offsetYLabel, () => Localization.OffsetY);

            var offsetXInput = UIFactory.CreateInputField("OffsetXInput", offsetRow.transform);
            UIFactory.SetupRightControl(offsetXInput.gameObject, new(80, 25), -220f);
            offsetXInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            offsetXInput.onValueChanged.AddListener(OnOffsetXValueChanged);
            offsetXInput.onEndEdit.AddListener(OnOffsetXEndEdit);
            _dcmButtonOffsetXInput = offsetXInput;

            var offsetXLabel = UIFactory.CreateText("OffsetXLabel", offsetRow.transform,
                Localization.OffsetX, 16, new Color(0.9f, 0.9f, 0.9f, 1));
            UIFactory.SetupRightLabel(offsetXLabel, 25f, -320f);
            UIFactory.SetupContentSizeFitter(offsetXLabel);
            UIFactory.SetLocalizedText(offsetXLabel, () => Localization.OffsetX);

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
            if (currentValue >= 0 && currentValue < _dcmButtonAnchorDropdown.options.Count)
            {
                _dcmButtonAnchorDropdown.value = -1;
                _dcmButtonAnchorDropdown.value = currentValue;
            }
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
            if (index >= 0 && index < anchorValues.Length)
            {
                UIConfig.DCMButtonAnchor = (AnchorPosition)anchorValues.GetValue(index);
                ConfigManager.SaveConfigToFile(UIConfig, "UIConfig.json");
                RefreshSettingsButton();
            }
        }

        private void OnOffsetXValueChanged(string value)
        {
            if (UIConfig == null) return;
            if (float.TryParse(value, out var offsetX))
            {
                UIConfig.DCMButtonOffsetX = offsetX;
                RefreshSettingsButton();
            }
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
            if (float.TryParse(value, out var offsetY))
            {
                UIConfig.DCMButtonOffsetY = offsetY;
                RefreshSettingsButton();
            }
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

        private GameObject CreateSettingRow(GameObject parent)
        {
            var row = new GameObject("SettingRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent.transform, false);

            var rowImage = row.GetComponent<Image>();
            var isEven = _settingRowIndex % 2 == 0;
            rowImage.color = isEven ? new(0.12f, 0.14f, 0.16f, 0.8f) : new(0.1f, 0.12f, 0.14f, 0.8f);
            _settingRowIndex++;

            UIFactory.SetupAnchor(row, new(0, 1), new(1, 1), new(0.5f, 1), new(800, 50), Vector2.zero);

            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 50;
            rowLayoutElement.minWidth = 800;
            rowLayoutElement.preferredWidth = 800;
            rowLayoutElement.flexibleWidth = 0;
            rowLayoutElement.flexibleHeight = 0;

            return row;
        }

        private void OnKeyButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForUIKeyInput = true;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey1Input = false;
            _isWaitingForModifierKey2Input = false;
            RefreshAllKeyButtons();
        }

        private void OnAnimatorParamsKeyButtonClicked()
        {
            if (UIConfig == null) return;
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
            _isWaitingForModifierKey1Input = true;
            _isWaitingForUIKeyInput = false;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey2Input = false;
            RefreshAllKeyButtons();
        }

        private void OnModifierKey2ButtonClicked()
        {
            if (UIConfig == null) return;
            _isWaitingForModifierKey2Input = true;
            _isWaitingForUIKeyInput = false;
            _isWaitingForAnimatorParamsKeyInput = false;
            _isWaitingForModifierKey1Input = false;
            RefreshAllKeyButtons();
        }

        private void HandleKeyInputCapture()
        {
            if ((!_isWaitingForUIKeyInput && !_isWaitingForAnimatorParamsKeyInput &&
                 !_isWaitingForModifierKey1Input && !_isWaitingForModifierKey2Input) || UIConfig == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isWaitingForUIKeyInput = false;
                _isWaitingForAnimatorParamsKeyInput = false;
                _isWaitingForModifierKey1Input = false;
                _isWaitingForModifierKey2Input = false;
                RefreshAllKeyButtons();
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode is KeyCode.Mouse0 or KeyCode.Mouse1 or KeyCode.Mouse2 or KeyCode.Mouse3
                        or KeyCode.Mouse4 or KeyCode.Mouse5 or KeyCode.Mouse6)
                        continue;

                    if (_isWaitingForUIKeyInput)
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
            if (PanelRoot == null) return;

            var updatePanel = new GameObject("UpdateCheckPanel", typeof(RectTransform));
            updatePanel.transform.SetParent(PanelRoot.transform, false);
            UIFactory.SetupRectTransform(updatePanel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(400f, 120f), pivot: new Vector2(1f, 0f), anchoredPosition: new Vector2(-20f, 20f));

            var updatePanelImage = updatePanel.AddComponent<Image>();
            updatePanelImage.color = new Color(0.1f, 0.12f, 0.14f, 0.9f);

            var outline = updatePanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new Vector2(1, -1);

            UIFactory.SetupVerticalLayoutGroup(updatePanel, 8f, new RectOffset(10, 10, 10, 10),
                childControlHeight: true, childControlWidth: true, childForceExpandHeight: false,
                childForceExpandWidth: true);

            var layoutElement = updatePanel.AddComponent<LayoutElement>();
            layoutElement.minWidth = 400f;
            layoutElement.minHeight = 120f;
            layoutElement.preferredWidth = 400f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            UIFactory.SetupContentSizeFitter(updatePanel, ContentSizeFitter.FitMode.Unconstrained);

            _updateInfoPanel = updatePanel;

            var updateInfoText = UIFactory.CreateText("UpdateInfo", updatePanel.transform, "", 18,
                new Color(0.9f, 0.9f, 0.9f, 1), TextAnchor.UpperCenter);

            var textComponent = updateInfoText.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.enableAutoSizing = true;
                textComponent.fontSizeMin = 12;
                textComponent.fontSizeMax = 20;
            }

            var updateInfoLayoutElement = updateInfoText.AddComponent<LayoutElement>();
            updateInfoLayoutElement.flexibleHeight = 0f;
            updateInfoLayoutElement.flexibleWidth = 1f;

            UIFactory.SetupContentSizeFitter(updateInfoText);

            var localizedUpdateInfo = updateInfoText.AddComponent<LocalizedText>();
            localizedUpdateInfo.SetTextGetter(GetUpdateInfoText);
            _updateInfoLocalizedText = localizedUpdateInfo;

            var changelogScrollView = UIFactory.CreateScrollView("ChangelogScrollView", updatePanel.transform,
                out var changelogContent);
            UIFactory.SetupRectTransform(changelogScrollView.gameObject, Vector2.zero, Vector2.one,
                offsetMax: new(-10, 0));

            var scrollViewLayout = changelogScrollView.gameObject.AddComponent<LayoutElement>();
            scrollViewLayout.minHeight = 0f;
            scrollViewLayout.preferredHeight = 200f;
            scrollViewLayout.flexibleHeight = 0f;
            scrollViewLayout.flexibleWidth = 1f;

            var changelogScrollbar = UIFactory.CreateScrollbar(changelogScrollView);
            changelogScrollbar.transform.SetParent(changelogScrollView.transform, false);

            changelogScrollView.gameObject.SetActive(false);

            UIFactory.SetupRectTransform(changelogContent, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero);

            UIFactory.SetupVerticalLayoutGroup(changelogContent, 0f, new RectOffset(0, 10, 0, 0),
                TextAnchor.UpperLeft, childForceExpandWidth: true);

            var changelogText = UIFactory.CreateText("ChangelogText", changelogContent.transform, "", 14,
                new Color(0.9f, 0.9f, 0.9f, 1), TextAnchor.UpperLeft);

            UIFactory.SetupRectTransform(changelogText, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero,
                pivot: new Vector2(0.5f, 1));

            var changelogTextComponent = changelogText.GetComponent<TextMeshProUGUI>();
            if (changelogTextComponent != null)
            {
                changelogTextComponent.enableWordWrapping = true;
                changelogTextComponent.overflowMode = TextOverflowModes.Overflow;
                changelogTextComponent.richText = true;
            }

            changelogText.AddComponent<LinkHandler>();

            UIFactory.SetupContentSizeFitter(changelogText, ContentSizeFitter.FitMode.Unconstrained);
            UIFactory.SetupContentSizeFitter(changelogContent, ContentSizeFitter.FitMode.Unconstrained);

            var scrollViewHeightAdjuster = changelogScrollView.gameObject.AddComponent<ScrollViewHeightAdjuster>();
            scrollViewHeightAdjuster.Initialize(changelogScrollView, changelogContent, 0f, 200f);

            _changelogText = changelogScrollView.gameObject;

            var downloadSection = new GameObject("DownloadSection", typeof(RectTransform));
            downloadSection.transform.SetParent(updatePanel.transform, false);
            downloadSection.SetActive(false);

            UIFactory.SetupVerticalLayoutGroup(downloadSection, 6f, new RectOffset(0, 0, 0, 0),
                childControlHeight: false, childControlWidth: true, childForceExpandHeight: false,
                childForceExpandWidth: true);

            var downloadLabel = UIFactory.CreateText("DownloadLabel", downloadSection.transform, "", 18,
                new Color(0.9f, 0.9f, 0.9f, 1), TextAnchor.UpperCenter);
            var downloadLabelTextComponent = downloadLabel.GetComponent<TextMeshProUGUI>();
            if (downloadLabelTextComponent != null)
            {
                downloadLabelTextComponent.enableAutoSizing = true;
                downloadLabelTextComponent.fontSizeMin = 12;
                downloadLabelTextComponent.fontSizeMax = 20;
            }

            UIFactory.SetLocalizedText(downloadLabel, () => Localization.DownloadLinks);
            UIFactory.SetupContentSizeFitter(downloadLabel);

            var downloadButtonsContainer = new GameObject("DownloadButtonsContainer", typeof(RectTransform));
            downloadButtonsContainer.transform.SetParent(downloadSection.transform, false);

            UIFactory.SetupHorizontalLayoutGroup(downloadButtonsContainer, 8f, new RectOffset(0, 0, 0, 0),
                TextAnchor.MiddleCenter,
                childControlHeight: false, childControlWidth: false, childForceExpandHeight: false,
                childForceExpandWidth: false);

            UIFactory.SetupContentSizeFitter(downloadButtonsContainer);

            _downloadButtonsContainer = downloadSection;

            var checkButton = UIFactory.CreateButton("UpdateCheckButton", updatePanel.transform,
                OnUpdateCheckButtonClicked,
                new Color(0.2f, 0.3f, 0.4f, 0.9f));

            var checkButtonLayoutElement = checkButton.AddComponent<LayoutElement>();
            checkButtonLayoutElement.minHeight = 30f;
            checkButtonLayoutElement.preferredHeight = 30f;
            checkButtonLayoutElement.preferredWidth = 120f;
            checkButtonLayoutElement.flexibleHeight = 0f;
            checkButtonLayoutElement.flexibleWidth = 0f;

            var checkButtonText = UIFactory.CreateText("Text", checkButton.transform, "", 16,
                Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(checkButtonText);
            var localizedCheckButtonText = checkButtonText.AddComponent<LocalizedText>();
            localizedCheckButtonText.SetTextGetter(GetCheckButtonText);
            _updateCheckButtonText = localizedCheckButtonText;

            _updateCheckButton = checkButton;

            if (UpdateChecker.Instance != null) UpdateChecker.OnUpdateCheckCompleted += OnUpdateCheckCompleted;

            _lastUpdateInfoRefreshTime = Time.time;
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

            if (_changelogText != null)
            {
                var hasChangelog = !string.IsNullOrEmpty(changelog);
                _changelogText.SetActive(hasChangelog);

                if (hasChangelog)
                {
                    var scrollView = _changelogText.GetComponent<ScrollRect>();
                    if (scrollView != null && scrollView.content != null)
                    {
                        var textComponent = scrollView.content.GetComponentInChildren<TextMeshProUGUI>();
                        if (textComponent != null)
                        {
                            var baseFontSize = (int)textComponent.fontSize;
                            textComponent.text = MarkdownToRichTextConverter.Convert(changelog!, baseFontSize, 20);
                        }
                    }
                }
            }

            if (_downloadButtonsContainer == null) return;

            var buttonsContainer = _downloadButtonsContainer.transform.Find("DownloadButtonsContainer");
            if (buttonsContainer != null)
                foreach (Transform child in buttonsContainer)
                    Destroy(child.gameObject);

            var hasDownloadLinks = downloadLinks is { Count: > 0 };
            _downloadButtonsContainer.SetActive(hasDownloadLinks);

            if (!hasDownloadLinks || buttonsContainer == null) return;
            foreach (var link in downloadLinks)
            {
                var downloadButton = UIFactory.CreateButton($"DownloadButton_{link.Name}",
                    buttonsContainer,
                    () => OpenURL(link.Url),
                    new Color(0.2f, 0.5f, 0.8f, 0.9f));

                UIFactory.SetupRectTransform(downloadButton, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(130f, 28f), pivot: new Vector2(0.5f, 0.5f));

                var buttonLayout = downloadButton.AddComponent<LayoutElement>();
                buttonLayout.preferredHeight = 28f;
                buttonLayout.preferredWidth = 130f;
                buttonLayout.flexibleHeight = 0f;
                buttonLayout.flexibleWidth = 0f;

                var buttonText = UIFactory.CreateText("Text", downloadButton.transform, link.Name, 12,
                    Color.white, TextAnchor.MiddleCenter);
                UIFactory.SetupButtonText(buttonText, 10, 14, 10f);
            }
        }
    }
}
