using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Data;
using DuckovCustomModel.UI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public partial class TargetSettingsPanel : MonoBehaviour
    {
        private readonly Dictionary<string, float> _scrollPositions = new(StringComparer.Ordinal);
        private GameObject? _clearRuntimeDataRow;
        private GameObject? _content;
        private TargetInfo? _currentTarget;
        private TMP_InputField? _idleAudioMaxIntervalInput;
        private TMP_InputField? _idleAudioMinIntervalInput;
        private GameObject? _modelHeightRow;
        private SliderWithInputToggle? _modelHeightSliderWithInput;
        private string? _renderedTargetId;
        private GameObject? _runtimeSettingsDivider;
        private ScrollRect? _scrollView;

        public void Initialize(Transform parent)
        {
            var scrollView = UIFactory.CreateScrollView("TargetSettingsScrollView", parent, out var content);
            UIFactory.SetupRectTransform(scrollView.gameObject, Vector2.zero, Vector2.one, Vector2.zero);

            var scrollbar = UIFactory.CreateScrollbar(scrollView, 6f, true);
            scrollbar.transform.SetParent(scrollView.transform, false);

            _content = content;
            _scrollView = scrollView;

            UIFactory.SetupVerticalLayoutGroup(_content, 10f, new(10, 10, 10, 10), TextAnchor.UpperCenter,
                true, true, true);
            UIFactory.SetupContentSizeFitter(_content, ContentSizeFitter.FitMode.Unconstrained);
            var contentLayoutElement = _content.AddComponent<LayoutElement>();
            contentLayoutElement.minWidth = 0;
            contentLayoutElement.preferredWidth = 0;
            contentLayoutElement.flexibleWidth = 1;
        }

        public void SetTarget(TargetInfo? targetInfo)
        {
            if (_currentTarget?.GetTargetTypeId() == targetInfo?.GetTargetTypeId() && _content != null &&
                _content.transform.childCount > 0)
            {
                _currentTarget = targetInfo;
                RefreshModelSelection();
                return;
            }

            SaveScrollPosition();
            _currentTarget = targetInfo;
            Refresh();
        }

        public void Refresh()
        {
            if (_content == null) return;
            if (_renderedTargetId == _currentTarget?.GetTargetTypeId()) SaveScrollPosition();
            _renderedTargetId = _currentTarget?.GetTargetTypeId();

            foreach (Transform child in _content.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (_currentTarget == null) return;

            BuildCharacterModelWarning();
            BuildSectionHeading(() => Localization.ModelSettings);
            BuildModelHeightSetting();
            BuildSocketScaleSection();
            BuildSectionHeading(() => Localization.EquipmentSettings);
            BuildHideEquipmentSetting();
            BuildSectionHeading(() => Localization.AudioSettings);
            BuildEnableModelAudioSetting();
            BuildModelAudioVolumeSetting();
            BuildEnableIdleAudioSetting();
            BuildIdleAudioIntervalSettings();
            BuildRuntimeSettingsDivider();
            BuildClearRuntimeDataButton();

            UpdateRuntimeSettingsVisibility();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_content.transform);
            if (_scrollView != null)
                _scrollView.verticalNormalizedPosition =
                    _scrollPositions.TryGetValue(_currentTarget.GetTargetTypeId(), out var position) ? position : 1;
        }

        public void RefreshModelSelection()
        {
            UpdateRuntimeSettingsVisibility();
        }

        private void BuildCharacterModelWarning()
        {
            if (_content == null || _currentTarget == null) return;

            var aiCharacterNameKey = _currentTarget.GetAICharacterNameKey();
            if (!_currentTarget.IsAICharacter() || aiCharacterNameKey == null)
                return;

            if (aiCharacterNameKey == AICharacters.AllAICharactersKey ||
                aiCharacterNameKey.StartsWith("Character_"))
                CreateWarningRow();
        }

        private void BuildModelHeightSetting()
        {
            if (_content == null || _currentTarget == null) return;

            _modelHeightRow = CreateSettingRow();

            var label = UIFactory.CreateText("Label", _modelHeightRow.transform, Localization.ModelHeight, 18,
                Color.white);
            SetupSettingLabel(label, 340);
            UIFactory.SetLocalizedText(label, () => Localization.ModelHeight);

            var controlsContainer =
                new GameObject("ControlsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            controlsContainer.transform.SetParent(_modelHeightRow.transform, false);
            SetupSettingControl(controlsContainer, new(300, 30));
            UIFactory.SetupHorizontalLayoutGroup(controlsContainer, 8f, new(0, 0, 0, 0), TextAnchor.MiddleRight,
                true);

            var sliderWithInputContainer =
                new GameObject("SliderWithInputContainer", typeof(RectTransform), typeof(LayoutElement));
            sliderWithInputContainer.transform.SetParent(controlsContainer.transform, false);
            var sliderLayoutElement = sliderWithInputContainer.GetComponent<LayoutElement>();
            sliderLayoutElement.minWidth = 0;
            sliderLayoutElement.preferredWidth = 0;
            sliderLayoutElement.flexibleWidth = 1;

            _modelHeightSliderWithInput = UIFactory.CreateSliderWithInputToggle(
                "ModelHeightSlider",
                sliderWithInputContainer.transform,
                0.5f, 10.0f, 1.0f,
                "F3",
                OnModelHeightChanged);
            UIFactory.SetupRectTransform(_modelHeightSliderWithInput.gameObject, Vector2.zero, Vector2.one,
                Vector2.zero);

            var resetButton = UIFactory.CreateButton("ResetHeightButton", controlsContainer.transform,
                OnResetHeightClicked, new Color(0.3f, 0.35f, 0.4f, 1)).GetComponent<Button>();
            var resetButtonLayoutElement = resetButton.gameObject.AddComponent<LayoutElement>();
            resetButtonLayoutElement.preferredWidth = 70;
            resetButtonLayoutElement.flexibleWidth = 0;
            var resetButtonText = UIFactory.CreateText("Text", resetButton.transform,
                Localization.Reset, 14, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(resetButtonText);
            UIFactory.SetLocalizedText(resetButtonText, () => Localization.Reset);
            UIFactory.SetupButtonColors(resetButton, new Color(1, 1, 1, 1), new Color(0.5f, 0.6f, 0.7f, 1),
                new Color(0.4f, 0.5f, 0.6f, 1), new Color(0.5f, 0.6f, 0.7f, 1));
        }

        private void BuildClearRuntimeDataButton()
        {
            if (_content == null || _currentTarget == null) return;

            _clearRuntimeDataRow = CreateSettingRow();

            var label = UIFactory.CreateText("Label", _clearRuntimeDataRow.transform,
                Localization.ClearCustomDataConfig, 18,
                Color.white);
            SetupSettingLabel(label, 130);
            UIFactory.SetLocalizedText(label, () => Localization.ClearCustomDataConfig);

            var clearButton = UIFactory.CreateButton("ClearRuntimeDataButton", _clearRuntimeDataRow.transform,
                OnClearRuntimeDataClicked, new Color(0.5f, 0.2f, 0.2f, 1)).GetComponent<Button>();
            SetupSettingControl(clearButton.gameObject, new(100, 30));
            var clearButtonText = UIFactory.CreateText("Text", clearButton.transform,
                Localization.Clear, 14, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(clearButtonText);
            UIFactory.SetLocalizedText(clearButtonText, () => Localization.Clear);
            UIFactory.SetupButtonColors(clearButton, new Color(1, 1, 1, 1), new Color(0.7f, 0.3f, 0.3f, 1),
                new Color(0.6f, 0.2f, 0.2f, 1), new Color(0.7f, 0.3f, 0.3f, 1));
        }

        private void BuildRuntimeSettingsDivider()
        {
            if (_content == null) return;

            var divider = new GameObject("RuntimeSettingsDivider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(_content.transform, false);

            var dividerImage = divider.GetComponent<Image>();
            dividerImage.color = new(0.3f, 0.35f, 0.4f, 0.8f);

            UIFactory.SetupRectTransform(divider, new(0, 1), new(1, 1), Vector2.zero,
                pivot: new Vector2(0.5f, 1));

            var dividerLayoutElement = divider.AddComponent<LayoutElement>();
            dividerLayoutElement.preferredHeight = 2;
            dividerLayoutElement.flexibleWidth = 1;
            dividerLayoutElement.flexibleHeight = 0;

            _runtimeSettingsDivider = divider;
        }

        private void UpdateRuntimeSettingsVisibility()
        {
            RefreshSocketScaleSettings();
            if (_modelHeightRow == null || _currentTarget == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            if (targetTypeId.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase))
            {
                _modelHeightRow.SetActive(false);
                if (_clearRuntimeDataRow != null)
                    _clearRuntimeDataRow.SetActive(false);
                if (_runtimeSettingsDivider != null)
                    _runtimeSettingsDivider.SetActive(false);
                return;
            }

            var usingModel = ModEntry.UsingModel;
            var modelID = usingModel?.GetModelID(targetTypeId);

            if (string.IsNullOrWhiteSpace(modelID) && _currentTarget.IsAICharacter())
                modelID = usingModel?.GetModelID(ModelTargetType.AllAICharacters);

            var hasModel = !string.IsNullOrWhiteSpace(modelID);
            var hasHelmetLocator = hasModel && ModelHeightManager.HasHelmetLocator(modelID!);

            _modelHeightRow.SetActive(hasHelmetLocator);
            if (_clearRuntimeDataRow != null)
                _clearRuntimeDataRow.SetActive(hasModel);
            if (_runtimeSettingsDivider != null)
                _runtimeSettingsDivider.SetActive(hasModel || hasHelmetLocator);

            if (!hasHelmetLocator || string.IsNullOrWhiteSpace(modelID)) return;
            var currentHeight = ModelHeightManager.GetHeight(targetTypeId, modelID);
            _modelHeightSliderWithInput?.SetValueWithoutNotify(currentHeight);
        }

        private void BuildHideEquipmentSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var hideEquipmentConfig = ModEntry.HideEquipmentConfig;

            var displayName = _currentTarget.DisplayName;
            var targetTypeId = _currentTarget.GetTargetTypeId();
            var aiCharacterNameKey = _currentTarget.GetAICharacterNameKey();
            var isAICharacter = _currentTarget.IsAICharacter();

            var label = UIFactory.CreateText("Label", settingRow.transform,
                string.Format(Localization.HideEquipment, displayName),
                18, Color.white);
            SetupSettingLabel(label, 60);

            UIFactory.SetLocalizedText(label, () =>
                string.Format(Localization.HideEquipment, displayName));

            var isOn = false;
            if (hideEquipmentConfig != null)
            {
                if (isAICharacter && aiCharacterNameKey != null)
                {
                    var aiTargetTypeId = ModelTargetType.CreateAICharacterTargetType(aiCharacterNameKey);
                    isOn = hideEquipmentConfig.GetHideEquipment(aiTargetTypeId) ||
                           hideEquipmentConfig.GetHideEquipment(ModelTargetType.AllAICharacters);
                }
                else
                {
                    isOn = hideEquipmentConfig.GetHideEquipment(targetTypeId);
                }
            }

            var toggle = UIFactory.CreateToggle("HideEquipmentToggle", settingRow.transform, isOn,
                OnHideEquipmentToggleChanged, true);
            SetupSettingControl(toggle.gameObject, new(20, 20));
        }

        private void BuildEnableModelAudioSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var modelAudioConfig = ModEntry.ModelAudioConfig;

            var label = UIFactory.CreateText("Label", settingRow.transform, Localization.EnableModelAudio, 18,
                Color.white);
            SetupSettingLabel(label, 60);
            UIFactory.SetLocalizedText(label, () => Localization.EnableModelAudio);

            var isOn = true;
            if (modelAudioConfig != null)
            {
                var targetTypeId = _currentTarget.GetTargetTypeId();
                isOn = modelAudioConfig.IsModelAudioEnabled(targetTypeId);
            }

            var toggle = UIFactory.CreateToggle("EnableModelAudioToggle", settingRow.transform, isOn,
                OnEnableModelAudioToggleChanged, true);
            SetupSettingControl(toggle.gameObject, new(20, 20));
        }

        private void BuildModelAudioVolumeSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var modelAudioConfig = ModEntry.ModelAudioConfig;

            var label = UIFactory.CreateText("Label", settingRow.transform, Localization.ModelAudioVolume, 18,
                Color.white);
            SetupSettingLabel(label, 240);
            UIFactory.SetLocalizedText(label, () => Localization.ModelAudioVolume);

            var volume = 1f;
            if (modelAudioConfig != null)
            {
                var targetTypeId = _currentTarget.GetTargetTypeId();
                volume = modelAudioConfig.GetModelAudioVolume(targetTypeId);
            }

            var sliderContainer = new GameObject("SliderContainer", typeof(RectTransform));
            sliderContainer.transform.SetParent(settingRow.transform, false);
            SetupSettingControl(sliderContainer, new(200, 30));

            var slider = UIFactory.CreateSliderWithInputToggle("ModelAudioVolumeSlider", sliderContainer.transform,
                0f, 100f, volume * 100, "F0", value => OnModelAudioVolumeChanged(value / 100));
            UIFactory.SetupRectTransform(slider.gameObject, Vector2.zero, Vector2.one, Vector2.zero);
        }

        private void BuildEnableIdleAudioSetting()
        {
            if (_content == null || _currentTarget == null) return;

            var settingRow = CreateSettingRow();
            var idleAudioConfig = ModEntry.IdleAudioConfig;

            var label = UIFactory.CreateText("Label", settingRow.transform, Localization.EnableIdleAudio, 18,
                Color.white);
            SetupSettingLabel(label, 60);
            UIFactory.SetLocalizedText(label, () => Localization.EnableIdleAudio);

            var isOn = false;
            if (idleAudioConfig != null)
            {
                var targetTypeId = _currentTarget.GetTargetTypeId();
                isOn = idleAudioConfig.IsIdleAudioEnabled(targetTypeId);
            }

            var toggle = UIFactory.CreateToggle("EnableIdleAudioToggle", settingRow.transform, isOn,
                OnEnableIdleAudioToggleChanged, true);
            SetupSettingControl(toggle.gameObject, new(20, 20));
        }

        private void BuildIdleAudioIntervalSettings()
        {
            if (_content == null || _currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var interval = idleAudioConfig.GetIdleAudioInterval(targetTypeId);

            BuildSectionHeading(() => Localization.IdleAudioInterval);
            var minIntervalRow = CreateSettingRow();
            var minLabel = UIFactory.CreateText("Label", minIntervalRow.transform,
                $"{Localization.MinValue} ({Localization.Seconds})", 18, Color.white);
            SetupSettingLabel(minLabel, 112);
            UIFactory.SetLocalizedText(minLabel, () =>
                $"{Localization.MinValue} ({Localization.Seconds})");

            _idleAudioMinIntervalInput =
                UIFactory.CreateInputField("IdleAudioMinIntervalInput", minIntervalRow.transform);
            _idleAudioMinIntervalInput.text = interval.Min.ToString("F1");
            if (_idleAudioMinIntervalInput.textComponent != null)
                _idleAudioMinIntervalInput.textComponent.enableAutoSizing = false;

            SetupSettingControl(_idleAudioMinIntervalInput.gameObject, new(80, 30));
            _idleAudioMinIntervalInput.onEndEdit.AddListener(OnIdleAudioMinIntervalChanged);

            var maxIntervalRow = CreateSettingRow();
            var maxLabel = UIFactory.CreateText("Label", maxIntervalRow.transform,
                $"{Localization.MaxValue} ({Localization.Seconds})", 18, Color.white);
            SetupSettingLabel(maxLabel, 112);
            UIFactory.SetLocalizedText(maxLabel, () =>
                $"{Localization.MaxValue} ({Localization.Seconds})");

            _idleAudioMaxIntervalInput =
                UIFactory.CreateInputField("IdleAudioMaxIntervalInput", maxIntervalRow.transform);
            _idleAudioMaxIntervalInput.text = interval.Max.ToString("F1");
            if (_idleAudioMaxIntervalInput.textComponent != null)
                _idleAudioMaxIntervalInput.textComponent.enableAutoSizing = false;

            SetupSettingControl(_idleAudioMaxIntervalInput.gameObject, new(80, 30));
            _idleAudioMaxIntervalInput.onEndEdit.AddListener(OnIdleAudioMaxIntervalChanged);
        }

        private GameObject CreateWarningText(Transform parent)
        {
            var warningText = UIFactory.CreateText("WarningText", parent,
                Localization.CharacterModelWarning, 16, new Color(1f, 0.8f, 0.4f, 1f), TextAnchor.UpperLeft);

            UIFactory.SetupRectTransform(warningText, new(0, 1), new(1, 1),
                offsetMin: new Vector2(20, 10), offsetMax: new Vector2(-20, -10),
                pivot: new Vector2(0.5f, 1));

            var warningTextComponent = warningText.GetComponent<TextMeshProUGUI>();
            if (warningTextComponent != null)
            {
                warningTextComponent.enableWordWrapping = true;
                warningTextComponent.overflowMode = TextOverflowModes.Overflow;
            }

            UIFactory.SetLocalizedText(warningText, () => Localization.CharacterModelWarning);

            var warningLayoutElement = warningText.AddComponent<LayoutElement>();
            warningLayoutElement.flexibleWidth = 1;

            UIFactory.SetupContentSizeFitter(warningText, ContentSizeFitter.FitMode.Unconstrained);

            return warningText;
        }

        private void CreateWarningRow()
        {
            if (_content == null) return;

            var row = new GameObject("WarningRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_content.transform, false);

            var rowImage = row.GetComponent<Image>();
            rowImage.color = new(0.3f, 0.2f, 0.05f, 0.9f);

            rowImage.raycastTarget = false;

            UIFactory.SetupRectTransform(row, new(0, 1), new(1, 1), Vector2.zero,
                pivot: new Vector2(0.5f, 1));

            UIFactory.SetupVerticalLayoutGroup(row, 0f, new(20, 20, 10, 10),
                TextAnchor.UpperLeft, true, true, true);

            var warningText = UIFactory.CreateText("WarningText", row.transform,
                Localization.CharacterModelWarning, 16, new Color(1f, 0.8f, 0.4f, 1f), TextAnchor.UpperLeft);

            UIFactory.SetupRectTransform(warningText, Vector2.zero, Vector2.one, Vector2.zero);

            var warningTextComponent = warningText.GetComponent<TextMeshProUGUI>();
            if (warningTextComponent != null) warningTextComponent.enableWordWrapping = true;

            UIFactory.SetLocalizedText(warningText, () => Localization.CharacterModelWarning);

            var warningLayoutElement = warningText.AddComponent<LayoutElement>();
            warningLayoutElement.flexibleWidth = 1;
            warningLayoutElement.flexibleHeight = 1;

            UIFactory.SetupContentSizeFitter(warningText, ContentSizeFitter.FitMode.Unconstrained);
            UIFactory.SetupContentSizeFitter(row, ContentSizeFitter.FitMode.Unconstrained);
        }

        private GameObject CreateSettingRow()
        {
            if (_content == null) return new();

            var row = new GameObject("SettingRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_content.transform, false);

            var rowImage = row.GetComponent<Image>();
            rowImage.color = UITheme.Surface;
            rowImage.raycastTarget = false;

            UIFactory.SetupAnchor(row, new(0, 1), new(1, 1), new Vector2(0.5f, 1), new Vector2(0, 50),
                Vector2.zero);

            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 44;
            rowLayoutElement.minWidth = 0;
            rowLayoutElement.preferredWidth = 0;
            rowLayoutElement.flexibleWidth = 1;
            rowLayoutElement.flexibleHeight = 0;

            return row;
        }

        private void SaveScrollPosition()
        {
            if (_scrollView != null && _currentTarget != null)
                _scrollPositions[_currentTarget.GetTargetTypeId()] = _scrollView.verticalNormalizedPosition;
        }

        private void BuildSectionHeading(Func<string> text)
        {
            if (_content == null) return;
            var heading = UIFactory.CreateText("SectionHeading", _content.transform, text(), 14, UITheme.Text);
            var layout = heading.AddComponent<LayoutElement>();
            layout.minWidth = 0;
            layout.preferredWidth = 0;
            layout.preferredHeight = 28;
            layout.flexibleWidth = 1;
            UIFactory.SetLocalizedText(heading, text);
        }

        private static void SetupSettingLabel(GameObject label, float reservedRight)
        {
            if (reservedRight < 200)
            {
                UIFactory.SetupBoundedLeftLabel(label, reservedRight);
                return;
            }

            label.transform.parent.GetComponent<LayoutElement>().preferredHeight = 84;
            UIFactory.SetupRectTransform(label, new(0, 1), Vector2.one,
                offsetMin: new(20, -42), offsetMax: new(-20, -6));
            var text = label.GetComponent<TMP_Text>();
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.TopLeft;
            label.AddComponent<StackedSettingLabel>();
        }

        private static void SetupSettingControl(GameObject control, Vector2 size)
        {
            var layout = control.transform.parent.GetComponent<LayoutElement>();
            if (layout == null || layout.preferredHeight < 70)
            {
                UIFactory.SetupRightControl(control, size);
                return;
            }

            UIFactory.SetupRectTransform(control, Vector2.zero, new(1, 0),
                offsetMin: new(20, 8), offsetMax: new(-20, 8 + size.y));
        }

        private void OnHideEquipmentToggleChanged(bool value)
        {
            if (_currentTarget == null) return;

            var hideEquipmentConfig = ModEntry.HideEquipmentConfig;
            if (hideEquipmentConfig == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var aiCharacterNameKey = _currentTarget.GetAICharacterNameKey();
            if (_currentTarget.IsAICharacter() && aiCharacterNameKey != null)
            {
                var aiTargetTypeId = ModelTargetType.CreateAICharacterTargetType(aiCharacterNameKey);
                hideEquipmentConfig.SetHideEquipment(aiTargetTypeId, value);
            }
            else
            {
                hideEquipmentConfig.SetHideEquipment(targetTypeId, value);
            }

            ConfigManager.SaveConfigToFile(hideEquipmentConfig, "HideEquipmentConfig.json");
        }

        private void OnEnableModelAudioToggleChanged(bool value)
        {
            if (_currentTarget == null) return;

            var modelAudioConfig = ModEntry.ModelAudioConfig;
            if (modelAudioConfig == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            modelAudioConfig.SetModelAudioEnabled(targetTypeId, value);

            ConfigManager.SaveConfigToFile(modelAudioConfig, "ModelAudioConfig.json");
        }

        private void OnEnableIdleAudioToggleChanged(bool value)
        {
            if (_currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            idleAudioConfig.SetIdleAudioEnabled(targetTypeId, value);

            ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
        }

        private void OnIdleAudioMinIntervalChanged(string value)
        {
            if (_currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            if (!float.TryParse(value, out var minValue)) return;
            if (minValue < 0.1f) minValue = 0.1f;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var interval = idleAudioConfig.GetIdleAudioInterval(targetTypeId);
            idleAudioConfig.SetIdleAudioInterval(targetTypeId, minValue, interval.Max);

            ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
        }

        private void OnIdleAudioMaxIntervalChanged(string value)
        {
            if (_currentTarget == null) return;

            var idleAudioConfig = ModEntry.IdleAudioConfig;
            if (idleAudioConfig == null) return;

            if (!float.TryParse(value, out var maxValue)) return;
            if (maxValue < 0.1f) maxValue = 0.1f;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var interval = idleAudioConfig.GetIdleAudioInterval(targetTypeId);
            if (maxValue < interval.Min) maxValue = interval.Min;
            idleAudioConfig.SetIdleAudioInterval(targetTypeId, interval.Min, maxValue);

            ConfigManager.SaveConfigToFile(idleAudioConfig, "IdleAudioConfig.json");
        }

        private void OnModelAudioVolumeChanged(float value)
        {
            if (_currentTarget == null) return;

            var modelAudioConfig = ModEntry.ModelAudioConfig;
            if (modelAudioConfig == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            modelAudioConfig.SetModelAudioVolume(targetTypeId, value);

            ConfigManager.SaveConfigToFile(modelAudioConfig, "ModelAudioConfig.json");
        }

        private void OnModelHeightChanged(float value)
        {
            if (_currentTarget == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var usingModel = ModEntry.UsingModel;
            var modelID = usingModel?.GetModelID(targetTypeId);

            if (string.IsNullOrWhiteSpace(modelID) && _currentTarget.IsAICharacter())
                modelID = usingModel?.GetModelID(ModelTargetType.AllAICharacters);

            if (string.IsNullOrWhiteSpace(modelID)) return;

            ModelHeightManager.SetHeight(targetTypeId, modelID, value);
        }

        private void OnResetHeightClicked()
        {
            if (_currentTarget == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var usingModel = ModEntry.UsingModel;
            var modelID = usingModel?.GetModelID(targetTypeId);

            if (string.IsNullOrWhiteSpace(modelID) && _currentTarget.IsAICharacter())
                modelID = usingModel?.GetModelID(ModelTargetType.AllAICharacters);

            if (string.IsNullOrWhiteSpace(modelID)) return;

            ModelHeightManager.ResetHeight(targetTypeId, modelID);

            var newHeight = ModelHeightManager.GetHeight(targetTypeId, modelID);
            _modelHeightSliderWithInput?.SetValueWithoutNotify(newHeight);
        }

        private void OnClearRuntimeDataClicked()
        {
            if (_currentTarget == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            var usingModel = ModEntry.UsingModel;
            var modelID = usingModel?.GetModelID(targetTypeId);

            if (string.IsNullOrWhiteSpace(modelID) && _currentTarget.IsAICharacter())
                modelID = usingModel?.GetModelID(ModelTargetType.AllAICharacters);

            if (string.IsNullOrWhiteSpace(modelID))
                return;

            ModelSocketScaleManager.ResetAll(targetTypeId, modelID);
            ModelRuntimeDataManager.ClearRuntimeData(targetTypeId, modelID);
            YsmRadialMenu.Instance?.ResetCurrentFormSettings(targetTypeId, modelID);

            ModelHeightManager.ResetHeight(targetTypeId, modelID);
            OnSocketScaleChanged(targetTypeId, modelID);

            UpdateRuntimeSettingsVisibility();

            ModLogger.Log($"Cleared runtime data for {targetTypeId}/{modelID}");
        }
    }
}
