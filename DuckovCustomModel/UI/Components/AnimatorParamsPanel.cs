using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Components
{
    public class AnimatorParamsPanel : MonoBehaviour
    {
        private const float MinWidth = 400f;
        private const float MinHeight = 300f;
        private const float DefaultWidth = 800f;
        private const float DefaultHeight = 600f;
        private const float RefreshDebounceDelay = 0.3f;
        private readonly Dictionary<int, AnimatorParamInfo> _cachedParamInfoDict = new();
        private readonly HashSet<string> _enabledTypes = ["float", "int", "bool"];
        private readonly HashSet<string> _enabledUsage = ["Used", "Unused"];

        private readonly Dictionary<int, bool> _paramIsChanging = new();
        private readonly Dictionary<int, GameObject> _paramItemObjects = new();
        private readonly Dictionary<int, object> _paramPreviousValues = new();
        private List<ModelHandler>? _availableHandlers;
        private List<AnimatorParamInfo>? _cachedParamInfos;
        private TMP_Dropdown? _characterDropdown;
        private GameObject? _closeButton;
        private GameObject? _contentArea;

        private GameObject? _panelRoot;
        private GameObject? _paramGridContent;
        private ScrollRect? _paramScrollRect;
        private CancellationTokenSource? _periodicRefreshCts;
        private CancellationTokenSource? _rebuildCacheAndGridCts;
        private CancellationTokenSource? _refreshCharacterListCts;
        private TMP_InputField? _searchField;
        private Regex? _searchRegex;

        private string _searchText = string.Empty;
        private ModelHandler? _selectedModelHandler;
        private GameObject? _titleBar;
        private MultiSelectDropdown? _typeFilterDropdown;
        private MultiSelectDropdown? _usageFilterDropdown;

        private void LateUpdate()
        {
            if (_panelRoot == null || !_panelRoot.activeSelf) return;
            RefreshParameterValues();
        }

        private void OnDestroy()
        {
            ModelManager.OnHandlerRegistered -= OnHandlerRegistered;
            ModelManager.OnHandlerUnregistered -= OnHandlerUnregistered;
            ModelListManager.OnModelChanged -= OnModelChanged;

            _refreshCharacterListCts?.Cancel();
            _refreshCharacterListCts?.Dispose();
            _refreshCharacterListCts = null;

            _rebuildCacheAndGridCts?.Cancel();
            _rebuildCacheAndGridCts?.Dispose();
            _rebuildCacheAndGridCts = null;

            _periodicRefreshCts?.Cancel();
            _periodicRefreshCts?.Dispose();
            _periodicRefreshCts = null;
        }

        public void Initialize(Transform parent)
        {
            CreatePanel(parent);
            BuildTitleBar();
            BuildContentArea();
            BuildParameterGrid();
            BuildFilters();
            RefreshCharacterList();
            ModelManager.OnHandlerRegistered += OnHandlerRegistered;
            ModelManager.OnHandlerUnregistered += OnHandlerUnregistered;
            ModelListManager.OnModelChanged += OnModelChanged;
            StartPeriodicRefresh();
        }

        private void OnHandlerRegistered(ModelHandler handler)
        {
            ScheduleRefreshCharacterList();
        }

        private void OnHandlerUnregistered(ModelHandler handler)
        {
            ScheduleRefreshCharacterList();
        }

        private void OnModelChanged(ModelChangedEventArgs e)
        {
            if (_panelRoot == null || !_panelRoot.activeSelf) return;
            if (_selectedModelHandler == null || e.Handler != _selectedModelHandler) return;
            ScheduleRebuildCacheAndGrid();
        }

        private void ScheduleRefreshCharacterList()
        {
            _refreshCharacterListCts?.Cancel();
            _refreshCharacterListCts?.Dispose();
            _refreshCharacterListCts = new();
            RefreshCharacterListWithSelectionAsync(_refreshCharacterListCts.Token).Forget();
        }

        private async UniTaskVoid RefreshCharacterListWithSelectionAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(RefreshDebounceDelay), cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            if (!IsVisible()) return;

            var previousHandler = _selectedModelHandler;
            RefreshCharacterListInternal();

            if (_characterDropdown == null || _availableHandlers == null) return;

            if (_availableHandlers.Count == 0)
            {
                ClearSelection();
                return;
            }

            var newIndex = previousHandler != null ? _availableHandlers.IndexOf(previousHandler) : -1;
            if (newIndex < 0) newIndex = 0;

            _characterDropdown.SetValueWithoutNotify(newIndex);
            _characterDropdown.RefreshShownValue();
            SelectHandler(newIndex);
        }

        private void ClearSelection()
        {
            _selectedModelHandler = null;
            _cachedParamInfos = null;
            _cachedParamInfoDict.Clear();
            _paramPreviousValues.Clear();
            _paramIsChanging.Clear();

            if (_paramGridContent == null) return;

            foreach (var item in _paramItemObjects.Values.OfType<GameObject>())
                Destroy(item);
            _paramItemObjects.Clear();
        }

        private void SelectHandler(int index)
        {
            if (_availableHandlers == null || index < 0 || index >= _availableHandlers.Count)
            {
                ClearSelection();
                return;
            }

            _selectedModelHandler = _availableHandlers[index];
            ScheduleRebuildCacheAndGrid();
        }

        public void Show()
        {
            if (_panelRoot == null) return;
            _panelRoot.SetActive(true);
            RefreshCharacterList();
            StartPeriodicRefresh();

            if (ConfigWindow.Instance != null)
                ConfigWindow.Instance.OnAnimatorParamsPanelOpened();
        }

        public void Hide()
        {
            if (_panelRoot == null) return;
            _panelRoot.SetActive(false);
            StopPeriodicRefresh();

            if (ConfigWindow.Instance != null)
                ConfigWindow.Instance.OnAnimatorParamsPanelClosed();
        }

        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void CreatePanel(Transform parent)
        {
            _panelRoot = UIFactory.CreateImage("AnimatorParamsPanel", parent, new Color(0.1f, 0.12f, 0.15f, 0.95f));
            var rectTransform = _panelRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new(0.5f, 0.5f);
            rectTransform.anchorMax = new(0.5f, 0.5f);
            rectTransform.pivot = new(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new(DefaultWidth, DefaultHeight);

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            var windowBase = _panelRoot.AddComponent<WindowBase>();
            windowBase.SetMinSize(new(MinWidth, MinHeight));
        }

        private void BuildTitleBar()
        {
            if (_panelRoot == null) return;

            _titleBar = UIFactory.CreateImage("TitleBar", _panelRoot.transform, new Color(0.15f, 0.18f, 0.22f, 0.9f));
            var titleRect = _titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new(0, 1);
            titleRect.anchorMax = new(1, 1);
            titleRect.pivot = new(0.5f, 1);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new(0, 40);

            var titleText = UIFactory.CreateText("Title", _titleBar.transform, Localization.AnimatorParameters, 18,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.SetLocalizedText(titleText, () => Localization.AnimatorParameters);
            var titleTextRect = titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = new(0, 0);
            titleTextRect.anchorMax = new(1, 1);
            titleTextRect.offsetMin = new(10, 0);
            titleTextRect.offsetMax = new(-100, 0);

            _closeButton = UIFactory.CreateButton("CloseButton", _titleBar.transform, Hide,
                new Color(0.8f, 0.2f, 0.2f, 1));
            var closeRect = _closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new(1, 0.5f);
            closeRect.anchorMax = new(1, 0.5f);
            closeRect.pivot = new(1, 0.5f);
            closeRect.anchoredPosition = new(-10, 0);
            closeRect.sizeDelta = new(80, 30);

            var closeText = UIFactory.CreateText("Text", _closeButton.transform, Localization.Close, 14,
                Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetLocalizedText(closeText, () => Localization.Close);
            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
        }

        private void BuildContentArea()
        {
            if (_panelRoot == null) return;

            _contentArea = new("ContentArea", typeof(RectTransform));
            _contentArea.transform.SetParent(_panelRoot.transform, false);
            var contentRect = _contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 0);
            contentRect.anchorMax = new(1, 1);
            contentRect.offsetMin = new(0, 0);
            contentRect.offsetMax = new(0, -40);
        }

        private void BuildFilters()
        {
            if (_contentArea == null) return;

            var filterContainer = new GameObject("FilterContainer", typeof(RectTransform));
            filterContainer.transform.SetParent(_contentArea.transform, false);
            var filterRect = filterContainer.GetComponent<RectTransform>();
            filterRect.anchorMin = new(0, 1);
            filterRect.anchorMax = new(1, 1);
            filterRect.pivot = new(0.5f, 1);
            filterRect.anchoredPosition = Vector2.zero;
            filterRect.offsetMin = new(0, -40);
            filterRect.offsetMax = new(0, 0);

            var layoutGroup = filterContainer.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new(5, 5, 0, 0);
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            _characterDropdown = UIFactory.CreateDropdown("CharacterDropdown", filterContainer.transform,
                OnCharacterChanged);
            var charLayout = _characterDropdown.gameObject.AddComponent<LayoutElement>();
            charLayout.preferredWidth = 300;
            charLayout.minWidth = 100;
            charLayout.flexibleWidth = 0;
            charLayout.minHeight = 30;
            charLayout.preferredHeight = 30;

            var typeFilterObj = new GameObject("TypeFilter", typeof(RectTransform), typeof(MultiSelectDropdown));
            typeFilterObj.transform.SetParent(filterContainer.transform, false);
            _typeFilterDropdown = typeFilterObj.GetComponent<MultiSelectDropdown>();
            var typeFilterLayout = typeFilterObj.AddComponent<LayoutElement>();
            typeFilterLayout.preferredWidth = 150;
            typeFilterLayout.minWidth = 100;
            typeFilterLayout.flexibleWidth = 0;
            typeFilterLayout.minHeight = 30;
            typeFilterLayout.preferredHeight = 30;
            var types = new[] { "float", "int", "bool", "trigger" };
            _typeFilterDropdown.Initialize(types, _enabledTypes, GetTypeDisplayName, OnTypeSelectionChanged);

            var usageFilterObj = new GameObject("UsageFilter", typeof(RectTransform), typeof(MultiSelectDropdown));
            usageFilterObj.transform.SetParent(filterContainer.transform, false);
            _usageFilterDropdown = usageFilterObj.GetComponent<MultiSelectDropdown>();
            var usageFilterLayout = usageFilterObj.AddComponent<LayoutElement>();
            usageFilterLayout.preferredWidth = 150;
            usageFilterLayout.minWidth = 100;
            usageFilterLayout.flexibleWidth = 0;
            usageFilterLayout.minHeight = 30;
            usageFilterLayout.preferredHeight = 30;
            var usages = new[] { "Used", "Unused" };
            _usageFilterDropdown.Initialize(usages, _enabledUsage, GetUsageDisplayName, OnUsageSelectionChanged);

            _searchField = UIFactory.CreateInputField("SearchField", filterContainer.transform, "Search...");
            var searchLayout = _searchField.gameObject.AddComponent<LayoutElement>();
            searchLayout.preferredWidth = 200;
            searchLayout.minWidth = 100;
            searchLayout.flexibleWidth = 1;
            searchLayout.minHeight = 30;
            searchLayout.preferredHeight = 30;
            _searchField.onValueChanged.AddListener(OnSearchChanged);
        }

        private void BuildParameterGrid()
        {
            if (_contentArea == null) return;

            _paramScrollRect = UIFactory.CreateScrollView("ParameterScrollView", _contentArea.transform,
                out _paramGridContent);
            var scrollRect = _paramScrollRect.GetComponent<RectTransform>();
            scrollRect.anchorMin = new(0, 0);
            scrollRect.anchorMax = new(1, 1);
            scrollRect.offsetMin = new(0, 0);
            scrollRect.offsetMax = new(0, -50);

            var scrollbar = UIFactory.CreateScrollbar(_paramScrollRect, 6f, true);
            scrollbar.transform.SetParent(_paramScrollRect.transform, false);

            var gridLayout = _paramGridContent.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new(240, 40);
            gridLayout.spacing = new(8, 4);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            var contentRect = _paramGridContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 1);
            contentRect.anchorMax = new(1, 1);
            contentRect.pivot = new(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.offsetMin = new(5, 0);
            contentRect.offsetMax = new(-5, 0);

            var contentSizeFitter = _paramGridContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RefreshCharacterListInternal()
        {
            if (_characterDropdown == null) return;

            _availableHandlers = [];
            _characterDropdown.options.Clear();

            if (LevelManager.Instance == null) return;

            var mainCharacter = LevelManager.Instance.MainCharacter;
            var mainCharacterPosition = mainCharacter != null && mainCharacter.gameObject.activeInHierarchy
                ? mainCharacter.transform.position
                : GameCamera.Instance != null
                    ? GameCamera.Instance.transform.position
                    : Vector3.zero;

            if (mainCharacter != null && mainCharacter.gameObject.activeInHierarchy)
            {
                var handler = mainCharacter.GetComponent<ModelHandler>();
                if (handler == null)
                    handler = ModelManager.InitializeModelHandler(mainCharacter, ModelTargetType.Character);
                if (handler != null && handler.CharacterMainControl != null &&
                    handler.CharacterMainControl.gameObject.activeInHierarchy)
                    _availableHandlers.Add(handler);
            }

            var petCharacter = LevelManager.Instance.PetCharacter;
            if (petCharacter != null && petCharacter.gameObject.activeInHierarchy)
            {
                var handler = petCharacter.GetComponent<ModelHandler>();
                if (handler == null)
                    handler = ModelManager.InitializeModelHandler(petCharacter, ModelTargetType.Pet);
                if (handler != null && handler.CharacterMainControl != null &&
                    handler.CharacterMainControl.gameObject.activeInHierarchy)
                    _availableHandlers.Add(handler);
            }

            var allHandlers = ModelManager.GetAllHandlers();
            foreach (var handler in allHandlers)
            {
                if (_availableHandlers.Contains(handler)) continue;
                if (handler.CustomAnimator == null) continue;

                var characterControl = handler.CharacterMainControl;
                if (characterControl == null) continue;
                if (!characterControl.gameObject.activeInHierarchy) continue;

                _availableHandlers.Add(handler);
            }

            _availableHandlers = _availableHandlers
                .OrderBy(handler =>
                {
                    if (handler.CharacterMainControl == null) return float.MaxValue;
                    var handlerPosition = handler.CharacterMainControl.transform.position;
                    return Vector3.Distance(mainCharacterPosition, handlerPosition);
                })
                .ToList();

            foreach (var handler in _availableHandlers)
            {
                var characterControl = handler.CharacterMainControl;
                if (characterControl == null) continue;

                var displayName = ModelTargetTypeRegistryExtensions.GetDisplayName(handler.GetTargetTypeId());
                var hash = handler.GetHashCode();
                var handlerName = string.IsNullOrEmpty(displayName) ? characterControl.name : displayName;
                if (string.IsNullOrEmpty(handlerName)) handlerName = "Unknown";

                var distance = Vector3.Distance(mainCharacterPosition, characterControl.transform.position);
                _characterDropdown.options.Add(new(
                    $"{handlerName} (#{hash:X8}) [{distance:F1}m]"));
            }

            _characterDropdown.RefreshShownValue();
        }

        private void StartPeriodicRefresh()
        {
            if (!IsVisible()) return;

            _periodicRefreshCts?.Cancel();
            _periodicRefreshCts?.Dispose();
            _periodicRefreshCts = new();
            PeriodicRefreshAsync(_periodicRefreshCts.Token).Forget();
        }

        private void StopPeriodicRefresh()
        {
            _periodicRefreshCts?.Cancel();
            _periodicRefreshCts?.Dispose();
            _periodicRefreshCts = null;
        }

        private async UniTaskVoid PeriodicRefreshAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;
                if (!IsVisible()) return;

                ScheduleRefreshCharacterList();
            }
        }

        private void RefreshCharacterList()
        {
            RefreshCharacterListInternal();

            if (_characterDropdown == null || _availableHandlers == null) return;

            if (_availableHandlers.Count == 0)
            {
                ClearSelection();
                return;
            }

            _characterDropdown.SetValueWithoutNotify(0);
            _characterDropdown.RefreshShownValue();
            SelectHandler(0);
        }

        private void OnCharacterChanged(int index)
        {
            SelectHandler(index);
        }

        private void OnSearchChanged(string text)
        {
            _searchText = text;
            _searchRegex = null;

            if (string.IsNullOrEmpty(text))
            {
                ScheduleRebuildCacheAndGrid();
                return;
            }

            if (text.StartsWith("/") && text.EndsWith("/") && text.Length > 2)
            {
                var pattern = text.Substring(1, text.Length - 2);
                if (!string.IsNullOrEmpty(pattern))
                    try
                    {
                        _searchRegex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    }
                    catch
                    {
                        _searchRegex = null;
                    }
            }

            ScheduleRebuildCacheAndGrid();
        }


        private static string GetTypeDisplayName(string type)
        {
            return type switch
            {
                "float" => Localization.AnimatorParamTypeFloat,
                "int" => Localization.AnimatorParamTypeInt,
                "bool" => Localization.AnimatorParamTypeBool,
                "trigger" => Localization.AnimatorParamTypeTrigger,
                _ => type,
            };
        }

        private static string GetUsageDisplayName(string usage)
        {
            return usage switch
            {
                "Used" => Localization.AnimatorParamUsageUsed,
                "Unused" => Localization.AnimatorParamUsageUnused,
                _ => usage,
            };
        }

        private void OnTypeSelectionChanged()
        {
            if (_typeFilterDropdown == null) return;
            _enabledTypes.Clear();
            foreach (var type in _typeFilterDropdown.GetSelectedValues())
                _enabledTypes.Add(type);
            ScheduleRebuildCacheAndGrid();
        }

        private void OnUsageSelectionChanged()
        {
            if (_usageFilterDropdown == null) return;
            _enabledUsage.Clear();
            foreach (var usage in _usageFilterDropdown.GetSelectedValues())
                _enabledUsage.Add(usage);
            ScheduleRebuildCacheAndGrid();
        }

        private void ScheduleRebuildCacheAndGrid()
        {
            _rebuildCacheAndGridCts?.Cancel();
            _rebuildCacheAndGridCts?.Dispose();
            _rebuildCacheAndGridCts = new();
            RebuildCacheAndGridAsync(_rebuildCacheAndGridCts.Token).Forget();
        }

        private async UniTaskVoid RebuildCacheAndGridAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(RefreshDebounceDelay), cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            if (_paramGridContent == null || _selectedModelHandler == null) return;

            _cachedParamInfos = null;
            UpdateAnimatorParamsCache(_selectedModelHandler);

            if (_cachedParamInfos == null || _cachedParamInfos.Count == 0) return;

            var filteredParams = FilterParameters(_cachedParamInfos);
            UpdateParameterGrid(filteredParams);
        }

        private void UpdateAnimatorParamsCache(ModelHandler modelHandler)
        {
            if (modelHandler == null)
            {
                var paramInfos = CustomAnimatorHash.GetAllParams();
                foreach (var param in paramInfos)
                    param.IsUsed = false;
                _cachedParamInfos = paramInfos;
                return;
            }

            var allParams = CustomAnimatorHash.GetAllParams();
            var allParamHashes = new HashSet<int>(allParams.Select(p => p.Hash));

            if (modelHandler.CustomAnimator == null)
            {
                foreach (var param in allParams)
                    param.IsUsed = false;
                _cachedParamInfos = allParams;
                return;
            }

            var animatorParamHashes = new HashSet<int>(
                modelHandler.CustomAnimator.parameters.Select(p => p.nameHash));

            var customParamsList = allParams.OrderBy(p => p.Name).ToList();
            foreach (var param in customParamsList)
                param.IsUsed = animatorParamHashes.Contains(param.Hash);

            var animatorParamsList = (from animatorParam in modelHandler.CustomAnimator.parameters
                where !allParamHashes.Contains(animatorParam.nameHash)
                let paramType = animatorParam.type switch
                {
                    AnimatorControllerParameterType.Float => "float",
                    AnimatorControllerParameterType.Int => "int",
                    AnimatorControllerParameterType.Bool => "bool",
                    AnimatorControllerParameterType.Trigger => "trigger",
                    _ => "unknown",
                }
                let initialValue = (object?)(animatorParam.type switch
                {
                    AnimatorControllerParameterType.Float => animatorParam.defaultFloat,
                    AnimatorControllerParameterType.Int => animatorParam.defaultInt,
                    AnimatorControllerParameterType.Bool => animatorParam.defaultBool,
                    _ => null,
                })
                select new AnimatorParamInfo
                {
                    Name = animatorParam.name,
                    Hash = animatorParam.nameHash,
                    Type = paramType,
                    InitialValue = initialValue,
                    IsExternal = true,
                    IsUsed = true,
                }).ToList();

            animatorParamsList = animatorParamsList.OrderBy(p => p.Name).ToList();

            var buffParamsList = new List<AnimatorParamInfo>();
            if (modelHandler.CurrentModelInfo?.BuffAnimatorParams != null)
                foreach (var (paramName, _) in modelHandler.CurrentModelInfo.BuffAnimatorParams)
                {
                    if (string.IsNullOrWhiteSpace(paramName)) continue;

                    var paramHash = Animator.StringToHash(paramName);
                    buffParamsList.Add(new()
                    {
                        Name = paramName,
                        Hash = paramHash,
                        Type = "bool",
                        InitialValue = false,
                        IsExternal = false,
                        IsUsed = animatorParamHashes.Contains(paramHash),
                    });
                }

            buffParamsList = buffParamsList.OrderBy(p => p.Name).ToList();

            var allParamInfos = customParamsList.Concat(animatorParamsList).Concat(buffParamsList).ToList();
            var seenHashes = new HashSet<int>();
            var uniqueParamInfos = allParamInfos.Where(paramInfo => seenHashes.Add(paramInfo.Hash)).ToList();

            _cachedParamInfos = uniqueParamInfos;
        }

        private List<AnimatorParamInfo> FilterParameters(List<AnimatorParamInfo> parameters)
        {
            return parameters.Where(param =>
            {
                if (_searchRegex != null)
                {
                    if (!_searchRegex.IsMatch(param.Name))
                        return false;
                }
                else if (!string.IsNullOrEmpty(_searchText))
                {
                    if (!param.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (!_enabledTypes.Contains(param.Type))
                    return false;

                var usage = param.IsUsed ? "Used" : "Unused";
                return _enabledUsage.Contains(usage);
            }).ToList();
        }

        private void UpdateParameterGrid(List<AnimatorParamInfo> parameters)
        {
            if (_paramGridContent == null) return;

            foreach (var item in _paramItemObjects.Values.OfType<GameObject>())
                Destroy(item);
            _paramItemObjects.Clear();
            _cachedParamInfoDict.Clear();
            _paramPreviousValues.Clear();
            _paramIsChanging.Clear();

            var processedHashes = new HashSet<int>();
            foreach (var paramInfo in parameters)
            {
                if (!processedHashes.Add(paramInfo.Hash)) continue;

                var paramItem = CreateParameterItem(paramInfo);
                if (paramItem == null) continue;

                _paramItemObjects[paramInfo.Hash] = paramItem;
                _cachedParamInfoDict[paramInfo.Hash] = paramInfo;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_paramGridContent.GetComponent<RectTransform>());
        }

        private GameObject? CreateParameterItem(AnimatorParamInfo paramInfo)
        {
            if (_paramGridContent == null) return null;

            var item = UIFactory.CreateImage($"Param_{paramInfo.Name}", _paramGridContent.transform,
                paramInfo.IsUsed ? new(0.15f, 0.2f, 0.25f, 0.8f) : new Color(0.1f, 0.1f, 0.1f, 0.2f));

            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = Vector2.zero;
            itemRect.pivot = new(0, 1);

            var paramText = HighlightWhitespace(paramInfo.Name, paramInfo.Type);
            var nameText = UIFactory.CreateText("NameText", item.transform, paramText, 12, Color.white);
            var nameTextComponent = nameText.GetComponent<TextMeshProUGUI>();
            nameTextComponent.enableWordWrapping = true;
            nameTextComponent.overflowMode = TextOverflowModes.Overflow;
            nameTextComponent.richText = true;
            var nameTextRect = nameText.GetComponent<RectTransform>();
            nameTextRect.anchorMin = new(0, 0);
            nameTextRect.anchorMax = new(1, 1);
            nameTextRect.offsetMin = new(5, 3);
            nameTextRect.offsetMax = new(-85, -3);

            var valueTextObj = UIFactory.CreateText("ValueText", item.transform, "", 12, Color.white,
                TextAnchor.MiddleRight);
            var valueTextComponent = valueTextObj.GetComponent<TextMeshProUGUI>();
            valueTextComponent.alignment = TextAlignmentOptions.MidlineRight;
            valueTextComponent.overflowMode = TextOverflowModes.Overflow;
            var valueTextRect = valueTextObj.GetComponent<RectTransform>();
            valueTextRect.anchorMin = new(1, 0);
            valueTextRect.anchorMax = new(1, 1);
            valueTextRect.pivot = new(1, 0.5f);
            valueTextRect.anchoredPosition = Vector2.zero;
            valueTextRect.sizeDelta = new(80, 0);
            valueTextRect.offsetMin = new(-80, 3);
            valueTextRect.offsetMax = new(-5, -3);

            return item;
        }

        private static string HighlightWhitespace(string paramName, string paramType)
        {
            var leadingWhitespace = 0;
            var trailingWhitespace = 0;

            foreach (var c in paramName)
                if (char.IsWhiteSpace(c))
                    leadingWhitespace++;
                else
                    break;

            if (leadingWhitespace == paramName.Length)
            {
                var result = new StringBuilder(paramName.Length * 32);
                result.Append("<color=#ff4444><b>");
                result.Append('␣', paramName.Length);
                result.Append("</b></color> (");
                result.Append(paramType);
                result.Append(')');
                return result.ToString();
            }

            for (var i = paramName.Length - 1; i >= leadingWhitespace; i--)
                if (char.IsWhiteSpace(paramName[i]))
                    trailingWhitespace++;
                else
                    break;

            if (leadingWhitespace == 0 && trailingWhitespace == 0)
                return $"{paramName} ({paramType})";

            var sb = new StringBuilder(paramName.Length + leadingWhitespace * 30 + trailingWhitespace * 30 + 32);

            if (leadingWhitespace > 0)
            {
                sb.Append("<color=#ff4444><b>");
                sb.Append('␣', leadingWhitespace);
                sb.Append("</b></color>");
            }

            sb.Append(paramName.AsSpan(leadingWhitespace, paramName.Length - leadingWhitespace - trailingWhitespace));

            if (trailingWhitespace > 0)
            {
                sb.Append("<color=#ff4444><b>");
                sb.Append('␣', trailingWhitespace);
                sb.Append("</b></color>");
            }

            sb.Append(" (");
            sb.Append(paramType);
            sb.Append(')');

            return sb.ToString();
        }

        private void RefreshParameterValues()
        {
            if (_selectedModelHandler == null || _paramGridContent == null) return;

            var customAnimatorControl = _selectedModelHandler.CustomAnimatorControl;
            var animator = _selectedModelHandler.CustomAnimator;

            foreach (var (hash, itemObj) in _paramItemObjects)
            {
                if (itemObj == null) continue;

                if (!_cachedParamInfoDict.TryGetValue(hash, out var paramInfo)) continue;

                var nameTextObj = itemObj.transform.Find("NameText");
                var valueTextObj = itemObj.transform.Find("ValueText");
                if (nameTextObj == null || valueTextObj == null) continue;

                var nameText = nameTextObj.GetComponent<TextMeshProUGUI>();
                var valueText = valueTextObj.GetComponent<TextMeshProUGUI>();
                if (nameText == null || valueText == null) continue;

                var valueTextStr = GetParameterValueString(paramInfo, customAnimatorControl, animator);
                var currentValue = GetParameterValueObject(customAnimatorControl, paramInfo, animator);

                UpdateParamState(paramInfo.Hash, currentValue, paramInfo.Type);

                var color = GetParameterColor(paramInfo, currentValue);
                nameText.color = color;
                valueText.color = color;
                valueText.text = valueTextStr;
            }
        }

        private static string GetParameterValueString(AnimatorParamInfo paramInfo,
            CustomAnimatorControl? customAnimatorControl, Animator? animator)
        {
            return GetParameterValue(customAnimatorControl, paramInfo, animator);
        }

        private static string GetParameterValue(CustomAnimatorControl? customAnimatorControl,
            AnimatorParamInfo paramInfo, Animator? animator)
        {
            if (!paramInfo.IsExternal)
            {
                if (customAnimatorControl == null) return "N/A";

                try
                {
                    return paramInfo.Type switch
                    {
                        "float" => customAnimatorControl.GetParameterFloat(paramInfo.Hash).ToString("F3"),
                        "int" => customAnimatorControl.GetParameterInteger(paramInfo.Hash).ToString(),
                        "bool" => customAnimatorControl.GetParameterBool(paramInfo.Hash).ToString(),
                        "trigger" => "Trigger",
                        _ => "Unknown",
                    };
                }
                catch
                {
                    return "N/A";
                }
            }

            if (animator == null) return "N/A";

            try
            {
                return paramInfo.Type switch
                {
                    "float" => animator.GetFloat(paramInfo.Hash).ToString("F3"),
                    "int" => animator.GetInteger(paramInfo.Hash).ToString(),
                    "bool" => animator.GetBool(paramInfo.Hash).ToString(),
                    "trigger" => "Trigger",
                    _ => "Unknown",
                };
            }
            catch
            {
                return "N/A";
            }
        }

        private static object? GetParameterValueObject(CustomAnimatorControl? customAnimatorControl,
            AnimatorParamInfo paramInfo, Animator? animator)
        {
            if (!paramInfo.IsExternal)
            {
                if (customAnimatorControl == null) return null;

                try
                {
                    return paramInfo.Type switch
                    {
                        "float" => customAnimatorControl.GetParameterFloat(paramInfo.Hash),
                        "int" => customAnimatorControl.GetParameterInteger(paramInfo.Hash),
                        "bool" => customAnimatorControl.GetParameterBool(paramInfo.Hash),
                        _ => null,
                    };
                }
                catch
                {
                    return null;
                }
            }

            if (animator == null) return null;

            try
            {
                return paramInfo.Type switch
                {
                    "float" => animator.GetFloat(paramInfo.Hash),
                    "int" => animator.GetInteger(paramInfo.Hash),
                    "bool" => animator.GetBool(paramInfo.Hash),
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        private void UpdateParamState(int hash, object? currentValue, string paramType)
        {
            if (currentValue == null || paramType == "trigger") return;

            if (!_paramPreviousValues.TryGetValue(hash, out var previousValue))
            {
                _paramPreviousValues[hash] = currentValue;
                _paramIsChanging[hash] = false;
                return;
            }

            var isValueChanged = !ValuesEqual(currentValue, previousValue, paramType);
            _paramIsChanging[hash] = isValueChanged;
            _paramPreviousValues[hash] = currentValue;
        }

        private static bool ValuesEqual(object? value1, object? value2, string paramType)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            return paramType switch
            {
                "float" => Math.Abs((float)value1 - (float)value2) < 0.0001f,
                "int" => (int)value1 == (int)value2,
                "bool" => (bool)value1 == (bool)value2,
                _ => value1.Equals(value2),
            };
        }

        private Color GetParameterColor(AnimatorParamInfo paramInfo, object? currentValue)
        {
            if (currentValue == null || paramInfo.InitialValue == null)
                return Color.white;

            var isChanged = !ValuesEqual(currentValue, paramInfo.InitialValue, paramInfo.Type);
            var isChanging = _paramIsChanging.GetValueOrDefault(paramInfo.Hash, false);

            if (isChanging)
                return new(1f, 0.4f, 0.1f, 1f);

            return isChanged ? new(1f, 0.8f, 0.2f, 1f) : Color.white;
        }
    }
}
