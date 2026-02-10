using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Data;
using Newtonsoft.Json.Linq;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class TargetListPanel : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _targetButtons = new();
        private GameObject? _content;
        private TMP_InputField? _searchInputField;
        private string _searchText = "";

        private TargetInfo? _selectedTarget;

        public event Action<TargetInfo>? OnTargetSelected;

        public void Initialize(Transform parent)
        {
            var searchInputField = UIFactory.CreateInputField("TargetSearchInput", parent,
                Localization.SearchTarget);
            UIFactory.SetupRectTransform(searchInputField.gameObject, new(0, 1), new(1, 1),
                offsetMin: new(0, -40), offsetMax: new(0, 0));

            _searchInputField = searchInputField;
            _searchInputField.onValueChanged.AddListener(OnSearchTextChanged);

            var scrollView = UIFactory.CreateScrollView("TargetListScrollView", parent, out var content);
            UIFactory.SetupRectTransform(scrollView.gameObject, new(0, 0), new(1, 1),
                offsetMin: new(0, 0), offsetMax: new(0, -50));

            var scrollViewImage = scrollView.GetComponent<Image>();
            if (scrollViewImage != null)
                scrollViewImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            var scrollbar = UIFactory.CreateScrollbar(scrollView, 6f, true);
            scrollbar.transform.SetParent(scrollView.transform, false);

            _content = content;

            UIFactory.SetupVerticalLayoutGroup(_content, 10f, new(10, 20, 0, 0), TextAnchor.UpperLeft, true, false,
                true);
            UIFactory.SetupContentSizeFitter(_content, ContentSizeFitter.FitMode.Unconstrained);
        }

        public void Refresh()
        {
            if (_content == null) return;

            var targets = GetAllTargets();
            var filteredTargets = FilterTargets(targets, _searchText);

            foreach (var target in filteredTargets)
                target.IsSelected = _selectedTarget != null && _selectedTarget.Id == target.Id;

            var existingButtonIds = new HashSet<string>(_targetButtons.Keys);
            var targetIds = new HashSet<string>(filteredTargets.Select(t => t.Id));

            foreach (var target in filteredTargets)
                if (_targetButtons.TryGetValue(target.Id, out var existingButton))
                {
                    existingButton.SetActive(true);
                    UpdateTargetButton(existingButton, target);
                }
                else
                {
                    BuildTargetButton(target);
                }

            foreach (var buttonId in existingButtonIds.Except(targetIds))
                if (_targetButtons.TryGetValue(buttonId, out var button))
                    button.SetActive(false);

            for (var i = 0; i < filteredTargets.Count; i++)
            {
                var target = filteredTargets[i];
                if (_targetButtons.TryGetValue(target.Id, out var button))
                    button.transform.SetSiblingIndex(i);
            }

            if (_selectedTarget != null || filteredTargets.Count <= 0) return;
            _selectedTarget = filteredTargets[0];
            _selectedTarget.IsSelected = true;
            if (_targetButtons.TryGetValue(_selectedTarget.Id, out var firstButton))
                UpdateTargetButton(firstButton, _selectedTarget);
            OnTargetSelected?.Invoke(_selectedTarget);
        }

        private static List<TargetInfo> GetAllTargets()
        {
            var targets = new List<TargetInfo>
            {
                TargetInfo.CreateCharacterTarget(),
                TargetInfo.CreatePetTarget(),
            };

            var extensionTargetTypes = ModelTargetTypeRegistry.GetAllAvailableTargetTypes()
                .Where(ModelTargetType.IsExtension);

            targets.AddRange(extensionTargetTypes.Select(targetTypeId =>
                TargetInfo.CreateFromTargetTypeId(targetTypeId, LocalizationManager.CurrentLanguage)));

            targets.Add(TargetInfo.CreateAllAICharactersTarget());

            targets.AddRange(from nameKey in AICharacters.SupportedAICharacters
                let displayName = LocalizationManager.GetPlainText(nameKey)
                select TargetInfo.CreateAICharacterTarget(nameKey, displayName));

            return targets;
        }

        private void BuildTargetButton(TargetInfo targetInfo)
        {
            if (_content == null) return;

            var buttonObj = UIFactory.CreateButton($"TargetButton_{targetInfo.Id}", _content.transform,
                () => OnTargetButtonClicked(targetInfo)).gameObject;
            UIFactory.SetupRectTransform(buttonObj, new(0, 0), new(1, 0), new(0, 50));

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50;
            layoutElement.preferredHeight = 50;
            layoutElement.flexibleWidth = 1;

            var text = UIFactory.CreateText("Text", buttonObj.transform, targetInfo.DisplayName, 18, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(text);

            SetupTargetButton(buttonObj, targetInfo);

            _targetButtons[targetInfo.Id] = buttonObj;
        }

        private static void UpdateTargetButton(GameObject buttonObj, TargetInfo targetInfo)
        {
            SetupTargetButton(buttonObj, targetInfo);
        }

        private static void SetupTargetButton(GameObject buttonObj, TargetInfo targetInfo)
        {
            var buttonImage = buttonObj.GetComponent<Image>();

            Color highlightedColor;
            Color pressedColor;
            Color outlineColor;

            var isExtension = targetInfo.IsExtension();

            if (targetInfo.HasModel)
            {
                if (isExtension)
                {
                    highlightedColor = new(0.3f, 0.65f, 0.65f, 1);
                    pressedColor = new(0.25f, 0.55f, 0.55f, 1);
                    outlineColor = new(0.2f, 0.45f, 0.45f, 0.8f);
                }
                else
                {
                    highlightedColor = new(0.5f, 0.8f, 0.6f, 1);
                    pressedColor = new(0.4f, 0.7f, 0.5f, 1);
                    outlineColor = new(0.3f, 0.6f, 0.4f, 0.8f);
                }
            }
            else if (targetInfo.HasFallbackModel)
            {
                if (isExtension)
                {
                    highlightedColor = new(0.6f, 0.75f, 0.9f, 1);
                    pressedColor = new(0.5f, 0.65f, 0.8f, 1);
                    outlineColor = new(0.4f, 0.55f, 0.7f, 0.8f);
                }
                else
                {
                    highlightedColor = new(0.7f, 0.6f, 0.9f, 1);
                    pressedColor = new(0.6f, 0.5f, 0.8f, 1);
                    outlineColor = new(0.5f, 0.4f, 0.7f, 0.8f);
                }
            }
            else
            {
                if (isExtension)
                {
                    highlightedColor = new(0.4f, 0.5f, 0.55f, 1);
                    pressedColor = new(0.3f, 0.4f, 0.45f, 1);
                    outlineColor = new(0.25f, 0.35f, 0.4f, 0.6f);
                }
                else
                {
                    highlightedColor = new(0.5f, 0.7f, 0.9f, 1);
                    pressedColor = new(0.4f, 0.6f, 0.8f, 1);
                    outlineColor = new(0.3f, 0.35f, 0.4f, 0.6f);
                }
            }

            buttonImage.color = highlightedColor;

            var outline = buttonObj.GetComponent<Outline>();
            if (outline == null)
                outline = buttonObj.AddComponent<Outline>();

            outline.effectColor = targetInfo.IsSelected ? new(1f, 0.5f, 0f, 1f) : outlineColor;
            outline.effectDistance = new(10, 0);

            var button = buttonObj.GetComponent<Button>();
            UIFactory.SetupButtonColors(button, highlightedColor, highlightedColor, pressedColor, highlightedColor);

            var textObj = buttonObj.transform.Find("Text");
            if (textObj == null) return;
            var text = textObj.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = targetInfo.DisplayName;
        }

        private void OnTargetButtonClicked(TargetInfo targetInfo)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                var info = new JObject
                {
                    ["DisplayName"] = targetInfo.DisplayName,
                    ["TargetTypeId"] = targetInfo.TargetTypeId,
                    ["ModelId"] = targetInfo.UsingModel,
                    ["FallbackModelId"] = targetInfo.UsingFallbackModel,
                };

                GUIUtility.systemCopyBuffer = info.ToString();
                return;
            }

            _selectedTarget = targetInfo;
            Refresh();
            OnTargetSelected?.Invoke(targetInfo);
        }

        private void OnSearchTextChanged(string searchText)
        {
            _searchText = searchText.Trim();
            Refresh();
        }

        private static List<TargetInfo> FilterTargets(List<TargetInfo> targets, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return targets;

            var searchLower = searchText.ToLower();
            return targets.Where(t =>
                t.DisplayName.ToLower().Contains(searchLower) ||
                t.Id.ToLower().Contains(searchLower)
            ).ToList();
        }
    }
}
