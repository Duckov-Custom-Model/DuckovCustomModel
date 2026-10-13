using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
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
        private readonly Dictionary<string, string> _modelNames = new();
        private readonly Dictionary<string, TargetInfo> _targetsById = new();
        private GameObject? _content;
        private List<TargetInfo> _filteredTargets = new();
        private float _searchAt;
        private TMP_InputField? _searchInputField;
        private bool _searchPending;
        private string _searchText = "";

        private TargetInfo? _selectedTarget;
        private VirtualVerticalList? _virtualList;

        private void Update()
        {
            if (!_searchPending || Time.unscaledTime < _searchAt) return;
            _searchPending = false;
            Refresh();
        }

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

            _virtualList = scrollView.gameObject.AddComponent<VirtualVerticalList>();
            _virtualList.Initialize(scrollView, 72, 4, new(2, 8, 4, 4), CreateTargetRow,
                (row, index) => UpdateTargetButton(row, _filteredTargets[index]));
        }

        public void Refresh()
        {
            if (_content == null) return;

            _modelNames.Clear();
            foreach (var bundle in ModelManager.ModelBundles)
            foreach (var model in bundle.Models)
                _modelNames[model.ModelID] = string.IsNullOrWhiteSpace(model.Name) ? model.ModelID : model.Name;
            var targets = GetAllTargets();
            _filteredTargets = FilterTargets(targets, _searchText);
            _targetsById.Clear();
            foreach (var target in targets)
                _targetsById[target.Id] = target;

            var selectedTargetTypeId = _selectedTarget?.TargetTypeId;
            var selectedDisplayName = _selectedTarget?.DisplayName;
            if (_selectedTarget != null)
                _targetsById.TryGetValue(_selectedTarget.Id, out _selectedTarget);
            var selectedTargetChanged = _selectedTarget != null &&
                                        (selectedTargetTypeId != _selectedTarget.TargetTypeId ||
                                         selectedDisplayName != _selectedTarget.DisplayName);

            var chooseInitial = _selectedTarget == null;
            _selectedTarget ??= _filteredTargets.FirstOrDefault() ?? targets.FirstOrDefault();
            foreach (var target in _filteredTargets)
                target.IsSelected = target.Id == _selectedTarget?.Id;
            _virtualList?.SetCount(_filteredTargets.Count);
            if ((chooseInitial || selectedTargetChanged) && _selectedTarget != null)
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

        private GameObject CreateTargetRow(Transform parent)
        {
            var row = UIFactory.CreateButton("TargetRow", parent);
            var item = row.AddComponent<TargetListItem>();
            item.Initialize();
            row.GetComponent<Button>().onClick.AddListener(() => OnTargetButtonClicked(item.TargetId));
            return row;
        }

        private void UpdateTargetButton(GameObject buttonObj, TargetInfo targetInfo)
        {
            var modelId = targetInfo.HasModel ? targetInfo.UsingModel : targetInfo.UsingFallbackModel;
            var modelName = _modelNames.TryGetValue(modelId, out var name) ? name : modelId;
            buttonObj.GetComponent<TargetListItem>().Bind(targetInfo,
                Regex.Replace(modelName, "§[0-9a-fk-or]", "", RegexOptions.IgnoreCase));
        }

        private void OnTargetButtonClicked(string targetId)
        {
            if (!_targetsById.TryGetValue(targetId, out var targetInfo)) return;
            if (InputBlocker.GetRealKey(KeyCode.LeftShift) || InputBlocker.GetRealKey(KeyCode.RightShift))
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

            if (_selectedTarget?.Id == targetId) return;
            if (_selectedTarget != null) _selectedTarget.IsSelected = false;
            _selectedTarget = targetInfo;
            targetInfo.IsSelected = true;
            _virtualList?.RefreshVisible();
            OnTargetSelected?.Invoke(targetInfo);
        }

        private void OnSearchTextChanged(string searchText)
        {
            var value = searchText.Trim();
            if (_searchText == value) return;
            _searchText = value;
            _searchPending = true;
            _searchAt = Time.unscaledTime + .15f;
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
