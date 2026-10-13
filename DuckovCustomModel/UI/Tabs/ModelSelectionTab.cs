using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Components;
using DuckovCustomModel.UI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Tabs
{
    public class ModelSelectionTab : Base.UIPanel
    {
        private CanvasGroup? _contentCanvasGroup;
        private FunctionButtonBar? _functionButtonBar;
        private ModelListPanel? _modelListPanel;
        private GameObject? _overlayPanel;
        private SearchField? _searchField;
        private TextMeshProUGUI? _targetHeading;
        private TargetListPanel? _targetListPanel;
        private TargetSettingsPanel? _targetSettingsPanel;

        protected override void OnDestroy()
        {
            ModelListManager.OnRefreshStarted -= OnRefreshStarted;
            ModelListManager.OnRefreshCompleted -= OnRefreshCompleted;
            base.OnDestroy();
        }

        protected override void CreatePanel()
        {
            PanelRoot = UIFactory.CreateImage("ModelSelectionPanel", transform, new Color(0.08f, 0.1f, 0.12f, 0.95f));
            UIFactory.SetupRectTransform(PanelRoot, new(0, 0), new(1, 1), Vector2.zero);
        }

        protected override void BuildContent()
        {
            if (PanelRoot == null) return;

            var content = CreateContent();
            var topContainer = CreateTopContainer(content);
            var bottomContainer = CreateBottomContainer(content);
            var leftContainer = CreateLeftContainer(bottomContainer);
            var midContainer = CreateMidContainer(bottomContainer);
            var rightContainer = CreateRightContainer(bottomContainer);
            bottomContainer.AddComponent<ModelColumnsLayout>().Initialize(
                (RectTransform)leftContainer.transform, (RectTransform)midContainer.transform,
                (RectTransform)rightContainer.transform);

            var functionButtonBarContainer = CreateFunctionButtonBarContainer(topContainer);
            var targetListContainer = CreateTargetListContainer(leftContainer);
            var noneModelButtonContainer = CreateNoneModelButtonContainer(midContainer);
            var searchFieldContainer = CreateSearchFieldContainer(midContainer);
            var bundleToolbarContainer = CreateBundleToolbarContainer(midContainer);
            var modelListContainer = CreateModelListContainer(midContainer);
            var targetSettingsContainer = CreateTargetSettingsContainer(rightContainer);
            var heading = UIFactory.CreateText("TargetHeading", rightContainer.transform, "", 18,
                UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.SetupRectTransform(heading, new(0, 1), Vector2.one, new(0, 36),
                pivot: new(.5f, 1), anchoredPosition: Vector2.zero);
            _targetHeading = heading.GetComponent<TextMeshProUGUI>();
            _targetHeading.margin = new(12, 0, 12, 0);
            _targetHeading.enableWordWrapping = false;
            _targetHeading.overflowMode = TextOverflowModes.Ellipsis;

            _functionButtonBar = functionButtonBarContainer.AddComponent<FunctionButtonBar>();
            _targetListPanel = targetListContainer.AddComponent<TargetListPanel>();
            _searchField = searchFieldContainer.AddComponent<SearchField>();
            _modelListPanel = modelListContainer.AddComponent<ModelListPanel>();
            _targetSettingsPanel = targetSettingsContainer.AddComponent<TargetSettingsPanel>();

            _functionButtonBar.Initialize(functionButtonBarContainer.transform);
            _targetListPanel.Initialize(targetListContainer.transform);
            _searchField.Initialize(searchFieldContainer.transform);
            _modelListPanel.Initialize(modelListContainer.transform, noneModelButtonContainer.transform);
            _modelListPanel.InitializeBundleToolbar(bundleToolbarContainer.transform);
            _targetSettingsPanel.Initialize(targetSettingsContainer.transform);

            _functionButtonBar.OnRefresh += () => { ModelListManager.RefreshModelList(forceReload: true); };
            _functionButtonBar.OnResetInvalidModels +=
                () => _modelListPanel?.Refresh(ModelListPanel.ScrollStrategy.ScrollToActiveModel);
            _targetListPanel.OnTargetSelected += targetInfo =>
            {
                if (_targetHeading != null) _targetHeading.text = targetInfo.DisplayName;
                _modelListPanel?.SetTarget(targetInfo);
                _targetSettingsPanel?.SetTarget(targetInfo);
            };
            _searchField.OnSearchChanged += searchText => _modelListPanel?.SetSearchText(searchText);
            _modelListPanel.OnModelSelected += () =>
            {
                _targetListPanel?.Refresh();
                _targetSettingsPanel?.RefreshModelSelection();
            };

            var contentRect = content.GetComponent<RectTransform>();
            if (contentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            _contentCanvasGroup = content.AddComponent<CanvasGroup>();
            CreateOverlayPanel();

            ModelListManager.OnRefreshStarted += OnRefreshStarted;
            ModelListManager.OnRefreshCompleted += OnRefreshCompleted;

            UpdateRefreshOverlay();
        }

        private void CreateOverlayPanel()
        {
            if (PanelRoot == null) return;

            _overlayPanel = UIFactory.CreateImage("OverlayPanel", PanelRoot.transform, new Color(0, 0, 0, 0.7f));
            UIFactory.SetupRectTransform(_overlayPanel, Vector2.zero, Vector2.one, Vector2.zero);
            _overlayPanel.transform.SetAsLastSibling();

            var overlayCanvasGroup = _overlayPanel.AddComponent<CanvasGroup>();
            overlayCanvasGroup.blocksRaycasts = true;
            overlayCanvasGroup.interactable = true;

            var overlayText = UIFactory.CreateText("OverlayText", _overlayPanel.transform, Localization.Updating, 24,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetupRectTransform(overlayText, Vector2.zero, Vector2.one, Vector2.zero);

            UIFactory.SetLocalizedText(overlayText, () => Localization.Updating);

            _overlayPanel.SetActive(false);
        }

        private void OnRefreshStarted()
        {
            _modelListPanel?.SuspendRefresh();
            UpdateRefreshOverlay();
        }

        private void OnRefreshCompleted()
        {
            UpdateRefreshOverlay();

            _targetListPanel?.Refresh();
            _modelListPanel?.Refresh(ModelListPanel.ScrollStrategy.ScrollToActiveModel, true);
            _targetSettingsPanel?.Refresh();
        }

        private void UpdateRefreshOverlay()
        {
            var isRefreshing = ModelListManager.IsRefreshing;

            if (_overlayPanel != null)
                _overlayPanel.SetActive(isRefreshing);

            if (_contentCanvasGroup == null) return;
            _contentCanvasGroup.interactable = !isRefreshing;
            _contentCanvasGroup.blocksRaycasts = !isRefreshing;
        }

        private GameObject CreateContent()
        {
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(PanelRoot!.transform, false);
            UIFactory.SetupRectTransform(content, Vector2.zero, Vector2.one, Vector2.zero,
                new(10, 10), new(-10, -10));
            return content;
        }

        private static GameObject CreateTopContainer(GameObject parent)
        {
            var container = CreateRegion("TopContainer", parent);
            UIFactory.SetupRectTransform(container, new(0, 1), Vector2.one, new(0, 56), pivot: new(.5f, 1));
            return container;
        }

        private static GameObject CreateBottomContainer(GameObject parent)
        {
            var container = CreateRegion("BottomContainer", parent);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero,
                offsetMax: new(0, -66));
            return container;
        }

        private static GameObject CreateLeftContainer(GameObject parent)
        {
            return CreateRegion("LeftContainer", parent);
        }

        private static GameObject CreateMidContainer(GameObject parent)
        {
            var container = UIFactory.CreateImage("MidContainer", parent.transform, UITheme.SurfaceAlternate);
            container.AddComponent<RectMask2D>();
            return container;
        }

        private static GameObject CreateRightContainer(GameObject parent)
        {
            return CreateRegion("RightContainer", parent);
        }

        private static GameObject CreateRegion(string name, GameObject parent)
        {
            var container = new GameObject(name, typeof(RectTransform));
            container.transform.SetParent(parent.transform, false);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero);
            return container;
        }

        private static GameObject CreateFunctionButtonBarContainer(GameObject parent)
        {
            var container = CreateRegion("FunctionButtonBarContainer", parent);
            return container;
        }

        private static GameObject CreateTargetListContainer(GameObject parent)
        {
            var container = CreateRegion("TargetListContainer", parent);
            return container;
        }

        private static GameObject CreateNoneModelButtonContainer(GameObject parent)
        {
            var container = CreateRegion("NoneModelButtonContainer", parent);
            UIFactory.SetupRectTransform(container, new(0, 1), Vector2.one, new(0, 34),
                pivot: new(.5f, 1), anchoredPosition: Vector2.zero);
            return container;
        }

        private static GameObject CreateBundleToolbarContainer(GameObject parent)
        {
            var container = CreateRegion("BundleToolbarContainer", parent);
            UIFactory.SetupRectTransform(container, new(0, 1), Vector2.one, new(0, 40),
                pivot: new(.5f, 1), anchoredPosition: new(0, -90));
            return container;
        }

        private static GameObject CreateSearchFieldContainer(GameObject parent)
        {
            var container = CreateRegion("SearchFieldContainer", parent);
            UIFactory.SetupRectTransform(container, new(0, 1), Vector2.one, new(0, 40),
                pivot: new(.5f, 1), anchoredPosition: new(0, -42));
            return container;
        }

        private static GameObject CreateModelListContainer(GameObject parent)
        {
            var container = CreateRegion("ModelListContainer", parent);
            container.AddComponent<Canvas>();
            container.AddComponent<GraphicRaycaster>();
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero,
                offsetMax: new(0, -138));
            return container;
        }

        private static GameObject CreateTargetSettingsContainer(GameObject parent)
        {
            var container = CreateRegion("TargetSettingsContainer", parent);
            UIFactory.SetupRectTransform(container, Vector2.zero, Vector2.one, Vector2.zero,
                offsetMax: new(0, -40));
            return container;
        }

        protected override void OnShow()
        {
            _targetListPanel?.Refresh();
            _modelListPanel?.RefreshSelection();
            _targetSettingsPanel?.Refresh();

            UpdateRefreshOverlay();
        }

        protected override void OnRefresh()
        {
            _targetListPanel?.Refresh();
            _modelListPanel?.Refresh(ModelListPanel.ScrollStrategy.ScrollToActiveModel, true);
            _targetSettingsPanel?.Refresh();
        }
    }
}
