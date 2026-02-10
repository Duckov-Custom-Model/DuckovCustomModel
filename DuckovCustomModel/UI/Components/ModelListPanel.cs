using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    public class ModelListPanel : MonoBehaviour
    {
        public enum ScrollStrategy
        {
            PreservePosition,
            ScrollToActiveModel,
            ScrollToTop,
        }

        private readonly Dictionary<string, GameObject> _bundleContainers = new();
        private readonly Dictionary<string, bool> _bundleExpandedStates = new();
        private readonly Dictionary<string, (bool isValid, string? errorMessage)> _bundleStatusCache = new();
        private readonly Dictionary<string, Toggle> _bundleToggles = new();
        private readonly List<ModelBundleInfo> _filteredModelBundles = [];
        private readonly Dictionary<string, GameObject> _loadingPlaceholders = new();
        private readonly Dictionary<string, RectTransform> _modelButtonRects = new();
        private GameObject? _content;
        private TargetInfo? _currentTarget;
        private bool _isThumbnailLoadingInProgress;
        private CancellationTokenSource? _refreshCancellationTokenSource;
        private float _savedScrollPosition;
        private ScrollRect? _scrollRect;
        private ScrollStrategy _scrollStrategy = ScrollStrategy.ScrollToTop;
        private string _searchText = string.Empty;

        private void OnDestroy()
        {
            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();
        }

        public event Action? OnModelSelected;

        public void Initialize(Transform parent, Transform? noneModelButtonParent = null)
        {
            var scrollView = UIFactory.CreateScrollView("ModelListScrollView", parent, out var content);
            UIFactory.SetupRectTransform(scrollView.gameObject, Vector2.zero, Vector2.one, Vector2.zero);

            var scrollbar = UIFactory.CreateScrollbar(scrollView, 6f, true);
            scrollbar.transform.SetParent(scrollView.transform, false);

            _scrollRect = scrollView;
            _content = content;

            UIFactory.SetupRectTransform(_content, new(0, 1), new(1, 1), Vector2.zero, pivot: new(0, 1),
                anchoredPosition: Vector2.zero);
            var contentLayoutElement = _content.AddComponent<LayoutElement>();
            contentLayoutElement.flexibleWidth = 1;

            UIFactory.SetupVerticalLayoutGroup(_content, 10f, new(10, 10, 10, 10), TextAnchor.UpperLeft);
            UIFactory.SetupContentSizeFitter(_content, ContentSizeFitter.FitMode.Unconstrained);

            if (noneModelButtonParent != null)
                BuildNoneModelButton(noneModelButtonParent);
        }

        public void InitializeBundleToolbar(Transform parent)
        {
            var toolbar = UIFactory.CreateImage("BundleToolbar", parent, new(0.15f, 0.18f, 0.22f, 0.9f));
            UIFactory.SetupRectTransform(toolbar, Vector2.zero, Vector2.one, Vector2.zero);
            UIFactory.SetupHorizontalLayoutGroup(toolbar, 10f, new(10, 10, 10, 10));

            var expandAllButton = UIFactory.CreateButton("ExpandAllBundlesButton", toolbar.transform,
                ExpandAllBundles, new(0.2f, 0.3f, 0.4f, 1)).GetComponent<Button>();
            UIFactory.SetupRectTransform(expandAllButton.gameObject, Vector2.zero, Vector2.zero, new(150, 0));
            var expandAllButtonLayoutElement = expandAllButton.gameObject.AddComponent<LayoutElement>();
            expandAllButtonLayoutElement.preferredWidth = 150;
            expandAllButtonLayoutElement.flexibleWidth = 0;
            expandAllButtonLayoutElement.flexibleHeight = 1;
            var expandAllTextObj = UIFactory.CreateText("Text", expandAllButton.transform,
                Localization.ExpandAllBundles, 16, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(expandAllTextObj);
            UIFactory.SetLocalizedText(expandAllTextObj, () => Localization.ExpandAllBundles);
            UIFactory.SetupButtonColors(expandAllButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1),
                new(0.3f, 0.4f, 0.5f, 1), new(0.4f, 0.5f, 0.6f, 1));

            var collapseAllButton = UIFactory.CreateButton("CollapseAllBundlesButton", toolbar.transform,
                CollapseAllBundles, new(0.2f, 0.3f, 0.4f, 1)).GetComponent<Button>();
            UIFactory.SetupRectTransform(collapseAllButton.gameObject, Vector2.zero, Vector2.zero, new(150, 0));
            var collapseAllButtonLayoutElement = collapseAllButton.gameObject.AddComponent<LayoutElement>();
            collapseAllButtonLayoutElement.preferredWidth = 150;
            collapseAllButtonLayoutElement.flexibleWidth = 0;
            collapseAllButtonLayoutElement.flexibleHeight = 1;
            var collapseAllTextObj = UIFactory.CreateText("Text", collapseAllButton.transform,
                Localization.CollapseAllBundles, 16, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(collapseAllTextObj);
            UIFactory.SetLocalizedText(collapseAllTextObj, () => Localization.CollapseAllBundles);
            UIFactory.SetupButtonColors(collapseAllButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1),
                new(0.3f, 0.4f, 0.5f, 1), new(0.4f, 0.5f, 0.6f, 1));

            var scrollToTopButton = UIFactory.CreateButton("ScrollToTopButton", toolbar.transform,
                ScrollToTop, new(0.2f, 0.3f, 0.4f, 1)).GetComponent<Button>();
            UIFactory.SetupRectTransform(scrollToTopButton.gameObject, Vector2.zero, Vector2.zero, new(120, 0));
            var scrollToTopButtonLayoutElement = scrollToTopButton.gameObject.AddComponent<LayoutElement>();
            scrollToTopButtonLayoutElement.preferredWidth = 120;
            scrollToTopButtonLayoutElement.flexibleWidth = 0;
            scrollToTopButtonLayoutElement.flexibleHeight = 1;
            var scrollToTopTextObj = UIFactory.CreateText("Text", scrollToTopButton.transform,
                Localization.ScrollToTop, 16, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(scrollToTopTextObj);
            UIFactory.SetLocalizedText(scrollToTopTextObj, () => Localization.ScrollToTop);
            UIFactory.SetupButtonColors(scrollToTopButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1),
                new(0.3f, 0.4f, 0.5f, 1), new(0.4f, 0.5f, 0.6f, 1));

            var scrollToBottomButton = UIFactory.CreateButton("ScrollToBottomButton", toolbar.transform,
                ScrollToBottom, new(0.2f, 0.3f, 0.4f, 1)).GetComponent<Button>();
            UIFactory.SetupRectTransform(scrollToBottomButton.gameObject, Vector2.zero, Vector2.zero, new(120, 0));
            var scrollToBottomButtonLayoutElement = scrollToBottomButton.gameObject.AddComponent<LayoutElement>();
            scrollToBottomButtonLayoutElement.preferredWidth = 120;
            scrollToBottomButtonLayoutElement.flexibleWidth = 0;
            scrollToBottomButtonLayoutElement.flexibleHeight = 1;
            var scrollToBottomTextObj = UIFactory.CreateText("Text", scrollToBottomButton.transform,
                Localization.ScrollToBottom, 16, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(scrollToBottomTextObj);
            UIFactory.SetLocalizedText(scrollToBottomTextObj, () => Localization.ScrollToBottom);
            UIFactory.SetupButtonColors(scrollToBottomButton, new(1, 1, 1, 1), new(0.4f, 0.5f, 0.6f, 1),
                new(0.3f, 0.4f, 0.5f, 1), new(0.4f, 0.5f, 0.6f, 1));
        }

        public void SetTarget(TargetInfo? targetInfo)
        {
            _currentTarget = targetInfo;
            Refresh(ScrollStrategy.ScrollToActiveModel);
        }

        public void SetSearchText(string searchText)
        {
            _searchText = searchText;
            Refresh();
        }

        public void Refresh(ScrollStrategy scrollStrategy = ScrollStrategy.ScrollToTop, bool forceRefresh = false)
        {
            if (_content == null) return;

            if (scrollStrategy == ScrollStrategy.PreservePosition && _scrollRect != null)
                _savedScrollPosition = _scrollRect.verticalNormalizedPosition;

            _scrollStrategy = scrollStrategy;

            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();

            foreach (Transform child in _content.transform) Destroy(child.gameObject);
            _bundleContainers.Clear();
            _bundleToggles.Clear();
            _modelButtonRects.Clear();
            foreach (var placeholder in _loadingPlaceholders.Values.OfType<GameObject>())
                Destroy(placeholder);
            _loadingPlaceholders.Clear();
            _filteredModelBundles.Clear();

            _refreshCancellationTokenSource = new();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _refreshCancellationTokenSource.Token,
                this.GetCancellationTokenOnDestroy()
            );

            RefreshModelListAsync(linkedCts.Token, linkedCts, forceRefresh).Forget();
        }

        private async UniTaskVoid RefreshModelListAsync(CancellationToken cancellationToken,
            CancellationTokenSource? linkedCts, bool forceFullRefresh)
        {
            try
            {
                if (_content == null) return;

                cancellationToken.ThrowIfCancellationRequested();

                if (forceFullRefresh) _bundleStatusCache.Clear();

                if (_currentTarget == null) return;

                var targetTypeId = _currentTarget.GetTargetTypeId();

                if (ModelTargetType.IsAICharacterTargetType(targetTypeId))
                {
                    var nameKey = ModelTargetType.ExtractAICharacterName(targetTypeId);
                    if (string.IsNullOrEmpty(nameKey)) return;

                    await BuildAICharacterModelListAsync(nameKey, targetTypeId, cancellationToken);
                }
                else
                {
                    var searchLower = _searchText.ToLowerInvariant();
                    foreach (var bundle in ModelManager.ModelBundles
                                 .Where(bundle => string.IsNullOrEmpty(searchLower)
                                                  || bundle.BundleName.ToLowerInvariant().Contains(searchLower)
                                                  || bundle.Models.Any(m => m.Name.ToLowerInvariant()
                                                                                .Contains(searchLower)
                                                                            || m.ModelID.ToLowerInvariant()
                                                                                .Contains(searchLower))))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var compatibleModels = bundle.Models
                            .Where(m => m.CompatibleWithTargetType(targetTypeId)).ToArray();
                        if (compatibleModels.Length <= 0) continue;
                        var filteredBundle = bundle.CreateFilteredCopy(compatibleModels);
                        _filteredModelBundles.Add(filteredBundle);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var bundlesCopy = _filteredModelBundles.ToList();

                    foreach (var bundle in bundlesCopy)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bundleKey = string.IsNullOrEmpty(bundle.DirectoryPath)
                            ? bundle.BundleName
                            : bundle.DirectoryPath;
                        CreateLoadingPlaceholder(bundleKey, bundle);
                    }

                    if (_content != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(_content.transform as RectTransform);

                    var buildTasks = bundlesCopy
                        .Select(bundle => BuildBundleGroupAsync(bundle, cancellationToken))
                        .ToArray();
                    await UniTask.WhenAll(buildTasks);

                    await UniTask.NextFrame(cancellationToken);
                    if (_content != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(_content.transform as RectTransform);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (forceFullRefresh) await LoadThumbnailsAsync(cancellationToken);

                await UniTask.NextFrame(cancellationToken);
                await UniTask.NextFrame(cancellationToken);

                if (_scrollRect != null)
                {
                    if (_content != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(_content.transform as RectTransform);
                    Canvas.ForceUpdateCanvases();
                    await UniTask.NextFrame(cancellationToken);

                    switch (_scrollStrategy)
                    {
                        case ScrollStrategy.PreservePosition:
                            _scrollRect.verticalNormalizedPosition = _savedScrollPosition;
                            break;
                        case ScrollStrategy.ScrollToActiveModel:
                            if (!ScrollToActiveModel())
                                _scrollRect.verticalNormalizedPosition = 1f;
                            break;
                        case ScrollStrategy.ScrollToTop:
                        default:
                            _scrollRect.verticalNormalizedPosition = 1f;
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                linkedCts?.Dispose();
            }
        }

        private async UniTask LoadThumbnailsAsync(CancellationToken cancellationToken)
        {
            if (_isThumbnailLoadingInProgress) return;

            _isThumbnailLoadingInProgress = true;
            try
            {
                foreach (var bundle in ModelManager.ModelBundles)
                foreach (var model in bundle.Models)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!ModelManager.TryGetThumbnail(bundle.DirectoryPath, model.ModelID, out var texture))
                    {
                        texture = await AssetBundleManager.LoadThumbnailTextureAsync(bundle, model, cancellationToken);
                        if (texture != null) ModelManager.CacheThumbnail(bundle.DirectoryPath, model.ModelID, texture);
                    }

                    if (bundle.Models.Length > 5) await UniTask.NextFrame(cancellationToken);
                }
            }
            finally
            {
                _isThumbnailLoadingInProgress = false;
            }
        }

        private async UniTask BuildAICharacterModelListAsync(string nameKey, string targetTypeId,
            CancellationToken cancellationToken)
        {
            if (_content == null) return;

            cancellationToken.ThrowIfCancellationRequested();
            var searchLower = _searchText.ToLowerInvariant();
            foreach (var bundle in ModelManager.ModelBundles
                         .Where(bundle => string.IsNullOrEmpty(searchLower)
                                          || bundle.BundleName.ToLowerInvariant().Contains(searchLower)
                                          || bundle.Models.Any(m => m.Name.ToLowerInvariant()
                                                                        .Contains(searchLower)
                                                                    || m.ModelID.ToLowerInvariant()
                                                                        .Contains(searchLower))))
            {
                cancellationToken.ThrowIfCancellationRequested();
                ModelInfo[] compatibleModels;
                if (nameKey == AICharacters.AllAICharactersKey)
                    compatibleModels = bundle.Models
                        .Where(m => m.CompatibleWithTargetType(targetTypeId)).ToArray();
                else
                    compatibleModels = bundle.Models
                        .Where(m => m.CompatibleWithTargetType(targetTypeId) || m.CompatibleWithAICharacter(nameKey))
                        .ToArray();

                if (compatibleModels.Length <= 0) continue;
                var filteredBundle = bundle.CreateFilteredCopy(compatibleModels);
                _filteredModelBundles.Add(filteredBundle);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var bundlesCopy = _filteredModelBundles.ToList();

            foreach (var bundle in bundlesCopy)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bundleKey = string.IsNullOrEmpty(bundle.DirectoryPath) ? bundle.BundleName : bundle.DirectoryPath;
                CreateLoadingPlaceholder(bundleKey, bundle);
            }

            if (_content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content.transform as RectTransform);

            var buildTasks = bundlesCopy.Select(bundle => BuildBundleGroupAsync(bundle, cancellationToken))
                .ToArray();
            await UniTask.WhenAll(buildTasks);
            await UniTask.NextFrame(cancellationToken);
            if (_content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content.transform as RectTransform);
        }

        private void BuildNoneModelButton(Transform parent)
        {
            var buttonObj = UIFactory.CreateButton("NoneModelButton", parent, OnNoneModelSelected,
                new(0.2f, 0.15f, 0.15f, 0.8f));
            UIFactory.SetupRectTransform(buttonObj, Vector2.zero, Vector2.one, Vector2.zero);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;
            layoutElement.flexibleWidth = 1;
            layoutElement.flexibleHeight = 0;

            var outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new(0.4f, 0.3f, 0.3f, 0.6f);
            outline.effectDistance = new(1, -1);

            var text = UIFactory.CreateText("Text", buttonObj.transform, Localization.NoModel, 16, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetupButtonText(text);

            var button = buttonObj.GetComponent<Button>();
            UIFactory.SetupButtonColors(button, new(1, 1, 1, 1), new(0.7f, 0.5f, 0.5f, 1),
                new(0.6f, 0.4f, 0.4f, 1), new(0.7f, 0.5f, 0.5f, 1));
        }

        private async UniTask BuildModelButtonAsync(ModelBundleInfo bundle, ModelInfo model,
            Transform parent, CancellationToken cancellationToken)
        {
            if (parent == null) return;

            var statusKey = $"{bundle.DirectoryPath}_{model.ModelID}";

            if (!_bundleStatusCache.TryGetValue(statusKey, out var statusResult))
            {
                statusResult = await AssetBundleManager.CheckBundleStatusAsync(bundle, model, cancellationToken);
                _bundleStatusCache[statusKey] = statusResult;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var (isValid, errorMessage) = statusResult;
            var hasError = !isValid;

            var usingModel = ModEntry.UsingModel;
            var isInUse = false;
            var isFallback = false;
            if (_currentTarget != null && usingModel != null)
            {
                isInUse = _currentTarget.UsingModel == model.ModelID;
                isFallback = _currentTarget.UsingFallbackModel == model.ModelID;
            }

            Color baseColor;
            if (isInUse && !hasError) baseColor = new(0.15f, 0.22f, 0.18f, 0.8f);
            else if (isFallback && !hasError) baseColor = new(0.18f, 0.15f, 0.22f, 0.8f);
            else baseColor = hasError ? new(0.22f, 0.15f, 0.15f, 0.8f) : new(0.15f, 0.18f, 0.22f, 0.8f);

            var buttonObj = UIFactory.CreateButton($"ModelButton_{model.ModelID}", parent, null, baseColor)
                .gameObject;

            UIFactory.SetupRectTransform(buttonObj, new(0, 0), new(1, 0), new(0, 150), pivot: new(0.5f, 0.5f));

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            _modelButtonRects[model.ModelID] = buttonRect;

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 150;
            layoutElement.preferredHeight = 150;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            var outline = buttonObj.AddComponent<Outline>();
            if (isInUse && !hasError)
                outline.effectColor = new(0.3f, 0.6f, 0.4f, 0.8f);
            else if (isFallback && !hasError)
                outline.effectColor = new(0.5f, 0.4f, 0.8f, 0.8f);
            else
                outline.effectColor = hasError
                    ? new(0.6f, 0.3f, 0.3f, 0.8f)
                    : new(0.3f, 0.35f, 0.4f, 0.6f);
            outline.effectDistance = new(1, -1);

            var thumbnailImage = UIFactory.CreateImage("Thumbnail", buttonObj.transform);
            UIFactory.SetupRectTransform(thumbnailImage, new(0, 0.5f), new(0, 0.5f), new(130, 130), pivot: new(0, 0.5f),
                anchoredPosition: new(10, 0));
            var thumbnailLayoutElement = thumbnailImage.AddComponent<LayoutElement>();
            thumbnailLayoutElement.minWidth = 130;
            thumbnailLayoutElement.minHeight = 130;
            thumbnailLayoutElement.preferredWidth = 130;
            thumbnailLayoutElement.preferredHeight = 130;
            thumbnailLayoutElement.flexibleWidth = 0;
            thumbnailLayoutElement.flexibleHeight = 0;
            var thumbnailImageComponent = thumbnailImage.GetComponent<Image>();

            Texture2D? texture;
            if (ModelManager.TryGetThumbnail(bundle.DirectoryPath, model.ModelID, out var cachedTexture))
            {
                texture = cachedTexture;
            }
            else
            {
                texture = await AssetBundleManager.LoadThumbnailTextureAsync(bundle, model, cancellationToken);
                if (texture != null) ModelManager.CacheThumbnail(bundle.DirectoryPath, model.ModelID, texture);
            }

            if (texture != null)
            {
                var sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height),
                    new(0.5f, 0.5f));
                thumbnailImageComponent.sprite = sprite;
                thumbnailImageComponent.preserveAspect = true;
                thumbnailImageComponent.color = Color.white;
            }
            else
            {
                thumbnailImageComponent.color = new(0.15f, 0.15f, 0.15f, 1);
                var thumbnailOutline = thumbnailImage.AddComponent<Outline>();
                thumbnailOutline.effectColor = new(0.3f, 0.3f, 0.3f, 0.5f);
                thumbnailOutline.effectDistance = new(1, -1);

                var placeholderText = UIFactory.CreateText("PlaceholderText", thumbnailImage.transform,
                    Localization.NoPreview, 15, new(0.6f, 0.6f, 0.6f, 1), TextAnchor.MiddleCenter);
                UIFactory.SetupRectTransform(placeholderText, Vector2.zero, Vector2.one, Vector2.zero);
            }

            var contentArea = new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(buttonObj.transform, false);
            UIFactory.SetupRectTransform(contentArea, new(0, 0), new(1, 1), offsetMin: new(150, 10),
                offsetMax: new(-10, -10));
            UIFactory.SetupVerticalLayoutGroup(contentArea, 2f, new(0, 0, 0, 0), TextAnchor.UpperLeft, true, true);

            var nameText = UIFactory.CreateText("Name", contentArea.transform,
                string.IsNullOrEmpty(model.Name) ? model.ModelID : model.Name, 20,
                hasError ? new(1f, 0.6f, 0.6f, 1) : Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetupRectTransform(nameText, new(0, 1), new(1, 1), new(0, 24), pivot: new(0, 1),
                anchoredPosition: Vector2.zero);
            var nameTextComponent = nameText.GetComponent<TextMeshProUGUI>();
            if (nameTextComponent != null)
            {
                nameTextComponent.enableWordWrapping = true;
                nameTextComponent.overflowMode = TextOverflowModes.Overflow;
            }

            var nameLayoutElement = nameText.AddComponent<LayoutElement>();
            nameLayoutElement.minHeight = 24;
            nameLayoutElement.flexibleHeight = 0;
            nameLayoutElement.flexibleWidth = 1;

            var infoText = UIFactory.CreateText("Info", contentArea.transform,
                Localization.GetModelInfo(model.ModelID, model.Author, model.Version), 16,
                hasError ? new(1f, 0.7f, 0.7f, 1) : new(0.8f, 0.8f, 0.8f, 1), TextAnchor.UpperLeft);
            UIFactory.SetupRectTransform(infoText, Vector2.zero, Vector2.one, new(0, 18));
            var infoTextComponent = infoText.GetComponent<TextMeshProUGUI>();
            if (infoTextComponent != null)
            {
                infoTextComponent.enableWordWrapping = true;
                infoTextComponent.overflowMode = TextOverflowModes.Truncate;
            }

            var infoLayoutElement = infoText.AddComponent<LayoutElement>();
            infoLayoutElement.minHeight = 18;
            infoLayoutElement.flexibleHeight = 0;
            infoLayoutElement.flexibleWidth = 1;

            if (hasError)
            {
                var errorText = UIFactory.CreateText("Error", contentArea.transform,
                    $"⚠ {(!string.IsNullOrEmpty(errorMessage) ? errorMessage : "Unknown error")}", 15,
                    new(1f, 0.4f, 0.4f, 1), TextAnchor.UpperLeft, FontStyle.Bold);
                var errorTextComponent = errorText.GetComponent<TextMeshProUGUI>();
                if (errorTextComponent != null)
                {
                    errorTextComponent.enableWordWrapping = true;
                    errorTextComponent.overflowMode = TextOverflowModes.Overflow;
                }

                UIFactory.SetupRectTransform(errorText, Vector2.zero, Vector2.one, new(0, 20));
                var errorLayoutElement = errorText.AddComponent<LayoutElement>();
                errorLayoutElement.minHeight = 20;
                errorLayoutElement.flexibleHeight = 1;
                UIFactory.SetupContentSizeFitter(errorText, ContentSizeFitter.FitMode.Unconstrained);
            }

            if (!string.IsNullOrEmpty(model.Description))
            {
                var descScrollView = UIFactory.CreateNonInteractiveScrollView("DescriptionScrollView",
                    contentArea.transform, out var descContent);
                UIFactory.SetupRectTransform(descScrollView.gameObject, Vector2.zero, Vector2.one, Vector2.zero);
                var descScrollViewLayout = descScrollView.gameObject.AddComponent<LayoutElement>();
                descScrollViewLayout.minHeight = 0f;
                descScrollViewLayout.preferredHeight = 70f;
                descScrollViewLayout.flexibleHeight = 0f;
                descScrollViewLayout.flexibleWidth = 1f;
                UIFactory.SetupRectTransform(descContent, new(0, 0), new(1, 1), Vector2.zero);
                UIFactory.SetupVerticalLayoutGroup(descContent, 0f, new(0, 0, 0, 0), TextAnchor.UpperLeft,
                    childForceExpandWidth: true);
                var descText = UIFactory.CreateText("Description", descContent.transform, model.Description, 15,
                    new(0.7f, 0.7f, 0.7f, 1), TextAnchor.UpperLeft);
                var descTextComponent = descText.GetComponent<TextMeshProUGUI>();
                if (descTextComponent != null)
                {
                    descTextComponent.enableWordWrapping = true;
                    descTextComponent.overflowMode = TextOverflowModes.Overflow;
                }

                UIFactory.SetupRectTransform(descText, new(0, 1), new(1, 1), Vector2.zero,
                    pivot: new Vector2(0.5f, 1));
                UIFactory.SetupContentSizeFitter(descText, ContentSizeFitter.FitMode.Unconstrained);
                UIFactory.SetupContentSizeFitter(descContent, ContentSizeFitter.FitMode.Unconstrained);
                var autoScrollView = descScrollView.gameObject.AddComponent<AutoScrollView>();
                var descContentRect = descContent.transform as RectTransform;
                if (descContentRect != null)
                    autoScrollView.Initialize(descScrollView, descContentRect, 70f);
            }

            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                if (isInUse && !hasError)
                    UIFactory.SetupButtonColors(button, new(1, 1, 1, 1), new(0.5f, 0.8f, 0.6f, 1),
                        new(0.4f, 0.7f, 0.5f, 1), new(0.5f, 0.8f, 0.6f, 1));
                else if (isFallback && !hasError)
                    UIFactory.SetupButtonColors(button, new(1, 1, 1, 1), new(0.7f, 0.6f, 0.9f, 1),
                        new(0.6f, 0.5f, 0.8f, 1), new(0.7f, 0.6f, 0.9f, 1));
                else
                    UIFactory.SetupButtonColors(button, new(1, 1, 1, 1),
                        hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1),
                        hasError ? new(0.6f, 0.4f, 0.4f, 1) : new(0.4f, 0.6f, 0.8f, 1),
                        hasError ? new(0.7f, 0.5f, 0.5f, 1) : new(0.5f, 0.7f, 0.9f, 1));

                button.interactable = !hasError;
                button.onClick.AddListener(() => SelectModel(model));
            }
        }

        private void SelectModel(ModelInfo model)
        {
            if (_currentTarget == null) return;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            ModelListManager.SetModelInConfig(targetTypeId, model.ModelID);

            OnModelSelected?.Invoke();
            Refresh(ScrollStrategy.PreservePosition);
        }

        private void OnNoneModelSelected()
        {
            if (_currentTarget == null) return;

            var usingModel = ModEntry.UsingModel;
            if (usingModel == null) return;

            var targetTypeId = _currentTarget.GetTargetTypeId();
            ModelListManager.SetModelInConfig(targetTypeId, string.Empty);

            OnModelSelected?.Invoke();
            Refresh(ScrollStrategy.PreservePosition);
        }

        private void CreateLoadingPlaceholder(string bundleKey, ModelBundleInfo bundle)
        {
            if (_content == null) return;

            var placeholderObj = new GameObject($"BundleGroup_Loading_{bundleKey}", typeof(RectTransform));
            placeholderObj.transform.SetParent(_content.transform, false);
            UIFactory.SetupRectTransform(placeholderObj, new(0, 0), new(1, 0), new(0, 0));
            UIFactory.SetupVerticalLayoutGroup(placeholderObj, 0f, new(0, 0, 0, 0), TextAnchor.UpperLeft, true, false,
                true);
            var placeholderLayoutElement = placeholderObj.AddComponent<LayoutElement>();
            placeholderLayoutElement.flexibleWidth = 1;
            UIFactory.SetupContentSizeFitter(placeholderObj, ContentSizeFitter.FitMode.Unconstrained);

            var placeholderHeader = UIFactory.CreateButton($"BundleHeader_Loading_{bundleKey}",
                placeholderObj.transform, null,
                new(0.18f, 0.2f, 0.25f, 0.9f)).gameObject;
            UIFactory.SetupRectTransform(placeholderHeader, new(0, 0), new(1, 0), offsetMin: new(0, 0),
                offsetMax: new(0, 50), pivot: new(0.5f, 0));
            var placeholderHeaderLayoutElement = placeholderHeader.AddComponent<LayoutElement>();
            placeholderHeaderLayoutElement.minHeight = 50;
            placeholderHeaderLayoutElement.preferredHeight = 50;
            placeholderHeaderLayoutElement.flexibleWidth = 1;
            placeholderHeaderLayoutElement.flexibleHeight = 0;
            var placeholderHeaderOutline = placeholderHeader.AddComponent<Outline>();
            placeholderHeaderOutline.effectColor = new(0.3f, 0.35f, 0.4f, 0.6f);
            placeholderHeaderOutline.effectDistance = new(1, -1);

            var loadingText = UIFactory.CreateText("LoadingText", placeholderHeader.transform,
                $"{Localization.Loading} {bundle.BundleName}", 18, new(0.7f, 0.7f, 0.7f, 1));
            UIFactory.SetupRectTransform(loadingText, new(0, 0), new(1, 1), offsetMin: new(50, 5),
                offsetMax: new(-10, -5));
            UIFactory.SetLocalizedText(loadingText, () => $"{Localization.Loading} {bundle.BundleName}");

            _loadingPlaceholders[bundleKey] = placeholderObj;
        }

        private async UniTask BuildBundleGroupAsync(ModelBundleInfo bundle, CancellationToken cancellationToken)
        {
            if (_content == null) return;

            cancellationToken.ThrowIfCancellationRequested();
            var bundleKey = string.IsNullOrEmpty(bundle.DirectoryPath) ? bundle.BundleName : bundle.DirectoryPath;
            _bundleExpandedStates.TryAdd(bundleKey, true);
            var isExpanded = _bundleExpandedStates[bundleKey];

            var usingModel = ModEntry.UsingModel;
            var hasModelInUse = false;
            var hasModelFallback = false;
            if (_currentTarget != null && usingModel != null)
            {
                var currentModelID = _currentTarget.UsingModel;
                hasModelInUse = bundle.Models.Any(m => m.ModelID == currentModelID);
                if (!hasModelInUse)
                {
                    currentModelID = _currentTarget.UsingFallbackModel;
                    hasModelFallback = bundle.Models.Any(m => m.ModelID == currentModelID);
                }
            }

            if (_scrollStrategy == ScrollStrategy.ScrollToActiveModel && (hasModelInUse || hasModelFallback))
            {
                isExpanded = true;
                _bundleExpandedStates[bundleKey] = true;
            }

            if (_scrollStrategy == ScrollStrategy.ScrollToActiveModel && (hasModelInUse || hasModelFallback))
            {
                isExpanded = true;
                _bundleExpandedStates[bundleKey] = true;
            }

            var bundleGroupObj = new GameObject($"BundleGroup_{bundleKey}", typeof(RectTransform));
            UIFactory.SetupRectTransform(bundleGroupObj, new(0, 0), new(1, 0), new(0, 0));
            UIFactory.SetupVerticalLayoutGroup(bundleGroupObj, 0f, new(0, 0, 0, 0), TextAnchor.UpperLeft, true, false,
                true);
            var bundleGroupLayoutElement = bundleGroupObj.AddComponent<LayoutElement>();
            bundleGroupLayoutElement.flexibleWidth = 1;
            UIFactory.SetupContentSizeFitter(bundleGroupObj, ContentSizeFitter.FitMode.Unconstrained);

            Color bundleHeaderColor;
            if (hasModelInUse)
                bundleHeaderColor = new(0.15f, 0.22f, 0.18f, 0.9f);
            else if (hasModelFallback)
                bundleHeaderColor = new(0.18f, 0.15f, 0.22f, 0.9f);
            else
                bundleHeaderColor = new(0.18f, 0.2f, 0.25f, 0.9f);
            var bundleHeaderObj = UIFactory.CreateButton($"BundleHeader_{bundleKey}", bundleGroupObj.transform, null,
                bundleHeaderColor).gameObject;
            UIFactory.SetupRectTransform(bundleHeaderObj, new(0, 0), new(1, 0), offsetMin: new(0, 0),
                offsetMax: new(0, 50), pivot: new(0.5f, 0));
            var bundleHeaderLayoutElement = bundleHeaderObj.AddComponent<LayoutElement>();
            bundleHeaderLayoutElement.minHeight = 50;
            bundleHeaderLayoutElement.preferredHeight = 50;
            bundleHeaderLayoutElement.flexibleWidth = 1;
            bundleHeaderLayoutElement.flexibleHeight = 0;
            var bundleHeaderOutline = bundleHeaderObj.AddComponent<Outline>();
            if (hasModelInUse)
                bundleHeaderOutline.effectColor = new(0.3f, 0.6f, 0.4f, 0.8f);
            else if (hasModelFallback)
                bundleHeaderOutline.effectColor = new(0.5f, 0.4f, 0.8f, 0.8f);
            else
                bundleHeaderOutline.effectColor = new(0.3f, 0.35f, 0.4f, 0.6f);
            bundleHeaderOutline.effectDistance = new(1, -1);

            var expandToggle = UIFactory.CreateToggle("ExpandToggle", bundleHeaderObj.transform, isExpanded,
                value => ToggleBundle(bundleKey, value).Forget());
            _bundleToggles[bundleKey] = expandToggle;
            UIFactory.SetupRectTransform(expandToggle.gameObject, new(0, 0.5f), new(0, 0.5f), new(30, 30),
                pivot: new(0, 0.5f), anchoredPosition: new(10, 0));
            var expandToggleImage = expandToggle.GetComponent<Image>();
            if (expandToggleImage != null)
                expandToggleImage.color = new(0.3f, 0.3f, 0.3f, 1);
            var expandIcon = UIFactory.CreateText("ExpandIcon", expandToggle.transform, isExpanded ? "▼" : "▶", 16,
                Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(expandIcon, Vector2.zero, Vector2.one, Vector2.zero);
            var expandIconText = expandIcon.GetComponent<TextMeshProUGUI>();
            if (expandIconText != null)
                expandIconText.raycastTarget = false;
            expandToggle.onValueChanged.AddListener(value =>
            {
                if (expandIconText != null) expandIconText.text = value ? "▼" : "▶";
            });

            var bundleNameText = UIFactory.CreateText("BundleName", bundleHeaderObj.transform,
                string.IsNullOrEmpty(bundle.BundleName) ? Localization.UnnamedBundle : bundle.BundleName, 18,
                Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.SetupRectTransform(bundleNameText, new(0, 0), new(1, 1), offsetMin: new(50, 5),
                offsetMax: new(-130, -5));

            var openBundleFolderButton = UIFactory.CreateButton("OpenBundleFolderButton", bundleHeaderObj.transform,
                () => OnOpenBundleFolderButtonClicked(bundle), new(0.3f, 0.5f, 0.3f, 1)).GetComponent<Button>();
            UIFactory.SetupRectTransform(openBundleFolderButton.gameObject, new(1, 0.5f), new(1, 0.5f), new(120, 35),
                pivot: new(1, 0.5f), anchoredPosition: new(-10, 0));
            var openBundleFolderTextObj = UIFactory.CreateText("Text", openBundleFolderButton.transform,
                Localization.OpenBundleFolder, 14, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(openBundleFolderTextObj, 12, 14);
            UIFactory.SetLocalizedText(openBundleFolderTextObj, () => Localization.OpenBundleFolder);
            UIFactory.SetupButtonColors(openBundleFolderButton, new(1, 1, 1, 1), new(0.4f, 0.6f, 0.4f, 1),
                new(0.3f, 0.5f, 0.3f, 1), new(0.4f, 0.6f, 0.4f, 1));

            var modelsContainer = new GameObject("ModelsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            modelsContainer.transform.SetParent(bundleGroupObj.transform, false);
            UIFactory.SetupRectTransform(modelsContainer, new(0, 0), new(1, 1), new(20, 0), new(-20, 0));
            UIFactory.SetupVerticalLayoutGroup(modelsContainer, 10f, new(0, 0, 10, 10), TextAnchor.UpperLeft, true,
                false, true);
            UIFactory.SetupContentSizeFitter(modelsContainer, ContentSizeFitter.FitMode.Unconstrained);
            var modelsContainerLayoutElement = modelsContainer.AddComponent<LayoutElement>();
            modelsContainerLayoutElement.flexibleWidth = 1;
            _bundleContainers[bundleKey] = modelsContainer;
            modelsContainer.SetActive(isExpanded);

            foreach (var model in bundle.Models.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await BuildModelButtonAsync(bundle, model, modelsContainer.transform, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_content != null)
            {
                if (_loadingPlaceholders.TryGetValue(bundleKey, out var placeholder))
                {
                    var siblingIndex = placeholder.transform.GetSiblingIndex();
                    Destroy(placeholder);
                    _loadingPlaceholders.Remove(bundleKey);
                    bundleGroupObj.transform.SetParent(_content.transform, false);
                    bundleGroupObj.transform.SetSiblingIndex(siblingIndex);
                }
                else
                {
                    bundleGroupObj.transform.SetParent(_content.transform, false);
                }
            }
        }

        private async UniTaskVoid ToggleBundle(string bundleKey, bool isExpanded)
        {
            _bundleExpandedStates[bundleKey] = isExpanded;
            if (_bundleToggles.TryGetValue(bundleKey, out var toggle))
            {
                toggle.SetIsOnWithoutNotify(isExpanded);
                var expandIcon = toggle.transform.Find("ExpandIcon");
                if (expandIcon != null)
                {
                    var expandIconText = expandIcon.GetComponent<TextMeshProUGUI>();
                    if (expandIconText != null)
                        expandIconText.text = isExpanded ? "▼" : "▶";
                }
            }

            if (!_bundleContainers.TryGetValue(bundleKey, out var container)) return;
            container.SetActive(isExpanded);

            await UniTask.NextFrame();
            if (_content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content.transform as RectTransform);
        }

        public void ExpandAllBundles()
        {
            foreach (var bundleKey in _bundleExpandedStates.Keys.ToList()
                         .Where(bundleKey => !_bundleExpandedStates[bundleKey]))
                ToggleBundle(bundleKey, true).Forget();
        }

        public void CollapseAllBundles()
        {
            foreach (var bundleKey in _bundleExpandedStates.Keys.ToList()
                         .Where(bundleKey => _bundleExpandedStates[bundleKey]))
                ToggleBundle(bundleKey, false).Forget();
        }

        public void ScrollToTop()
        {
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        public void ScrollToBottom()
        {
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
        }

        private bool ScrollToActiveModel()
        {
            if (_scrollRect == null || _currentTarget == null) return false;

            var targetModelId = _currentTarget.UsingModel;
            if (string.IsNullOrEmpty(targetModelId))
                targetModelId = _currentTarget.UsingFallbackModel;

            if (string.IsNullOrEmpty(targetModelId))
                return false;

            if (!_modelButtonRects.TryGetValue(targetModelId, out var buttonRect) || buttonRect == null)
                return false;

            var contentRect = _content?.GetComponent<RectTransform>();
            if (contentRect == null)
                return false;

            var viewportRect = _scrollRect.viewport ?? _scrollRect.GetComponent<RectTransform>();
            if (viewportRect == null)
                return false;

            Canvas.ForceUpdateCanvases();

            var contentHeight = contentRect.rect.height;
            var viewportHeight = viewportRect.rect.height;

            if (contentHeight <= viewportHeight)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                return true;
            }

            var buttonWorldPos = buttonRect.TransformPoint(Vector3.zero);
            var buttonLocalPosInContent = contentRect.InverseTransformPoint(buttonWorldPos);

            var buttonCenterFromTop = -buttonLocalPosInContent.y + buttonRect.rect.height / 2;
            var viewportCenter = viewportHeight / 2;
            var desiredViewportTop = buttonCenterFromTop - viewportCenter;

            var scrollableHeight = contentHeight - viewportHeight;

            if (scrollableHeight <= 0)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                return true;
            }

            var normalizedPosition = Mathf.Clamp01(desiredViewportTop / scrollableHeight);
            _scrollRect.verticalNormalizedPosition = 1f - normalizedPosition;

            return true;
        }

        private static void OnOpenBundleFolderButtonClicked(ModelBundleInfo bundle)
        {
            try
            {
                var bundleDirectory = bundle.DirectoryPath;
                if (string.IsNullOrEmpty(bundleDirectory) || !Directory.Exists(bundleDirectory))
                {
                    ModLogger.LogWarning($"Bundle directory not found: {bundleDirectory}");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = bundleDirectory,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to open bundle folder: {ex.Message}");
            }
        }
    }
}
