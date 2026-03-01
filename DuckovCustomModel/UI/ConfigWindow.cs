using System;
using System.Collections;
using Duckov.UI;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Components;
using DuckovCustomModel.UI.Tabs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckovCustomModel.UI
{
    public class ConfigWindow : MonoBehaviour
    {
        private AnimatorParamsPanel? _animatorParamsPanel;

        private CharacterInputControl? _charInput;
        private bool _charInputWasEnabled;
        private bool _cursorWasVisible;
        private bool _emotionModifierBlockingInput;
        private bool _isInitialized;

        private AnchorPosition _lastAnchorPosition;
        private float _lastOffsetX;
        private float _lastOffsetY;
        private ModelSelectionTab? _modelSelectionTab;
        private CursorLockMode _originalCursorLockState;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private PlayerInput? _playerInput;
        private bool _playerInputWasActive;
        private GameObject? _settingsButton;
        private SettingsTab? _settingsTab;
        private bool _showAnimatorParamsWindow;
        private TabSystem? _tabSystem;
        private bool _uiActive;
        private GameObject? _uiRoot;
        private GameObject? _updateIndicatorButton;
        private GameObject? _updateIndicatorTitle;

        public static ConfigWindow? Instance { get; private set; }

        public int EmotionParameterValue1 { get; private set; }
        public int EmotionParameterValue2 { get; private set; }

        private static UIConfig? UIConfig => ModEntry.UIConfig;
        private static CharacterInputControl? CharacterInputControl => CharacterInputControl.Instance;
        private static PlayerInput? PlayerInput => GameManager.MainPlayerInput;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            var uiConfig = UIConfig;
            if (uiConfig == null) return;

            if (!_isInitialized)
            {
                InitializeUI();
                return;
            }

            UpdateSettingsButtonVisibility();
            UpdateUpdateIndicators();

            if (uiConfig.DCMButtonAnchor != _lastAnchorPosition ||
                Math.Abs(uiConfig.DCMButtonOffsetX - _lastOffsetX) > 0.01f ||
                Math.Abs(uiConfig.DCMButtonOffsetY - _lastOffsetY) > 0.01f)
            {
                _lastAnchorPosition = uiConfig.DCMButtonAnchor;
                _lastOffsetX = uiConfig.DCMButtonOffsetX;
                _lastOffsetY = uiConfig.DCMButtonOffsetY;
                RefreshSettingsButton();
            }

            if (IsTypingInInputField() || _panelRoot == null) return;

            if (_settingsTab != null && _settingsTab.IsWaitingForKeyInput) return;

            if (Input.GetKeyDown(uiConfig.ToggleKey))
            {
                if (_panelRoot.activeSelf)
                    HidePanel();
                else
                    ShowPanel();
            }

            if (uiConfig.AnimatorParamsToggleKey != KeyCode.None &&
                Input.GetKeyDown(uiConfig.AnimatorParamsToggleKey))
            {
                if (!_isInitialized) InitializeUI();
                _showAnimatorParamsWindow = !_showAnimatorParamsWindow;
                SetAnimatorParamsWindowVisible(_showAnimatorParamsWindow);
                _settingsTab?.RefreshAnimatorParamsToggleState(_showAnimatorParamsWindow);
            }

            HandleShortcutParameters(uiConfig);

            if (_uiActive && Input.GetKeyDown(KeyCode.Escape)) HidePanel();

            if (!_uiActive) return;

            if (_charInput != null && _charInputWasEnabled && _charInput.enabled) _charInput.enabled = false;

            if (_playerInput != null && _playerInputWasActive && _playerInput.inputIsActive)
                _playerInput.DeactivateInput();
        }

        private void LateUpdate()
        {
            if (!_uiActive || _panelRoot == null || !_panelRoot.activeSelf) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            UpdateChecker.OnUpdateCheckCompleted -= OnUpdateCheckCompleted;
            ModelListManager.OnModelChanged -= OnModelChanged;
        }


        private void InitializeUI()
        {
            if (_isInitialized) return;

            CreateOrFindUiRoot();
            BuildMainPanel();
            SetupPanels();
            BuildSettingsButton();
            BuildAnimatorParamsPanel();

            _uiActive = false;
            if (_overlay != null) _overlay.SetActive(false);
            if (_panelRoot != null) _panelRoot.SetActive(false);

            _isInitialized = true;

            var uiConfig = UIConfig;
            if (uiConfig != null)
            {
                _lastAnchorPosition = uiConfig.DCMButtonAnchor;
                _lastOffsetX = uiConfig.DCMButtonOffsetX;
                _lastOffsetY = uiConfig.DCMButtonOffsetY;
            }

            if (UpdateChecker.Instance != null)
            {
                UpdateChecker.OnUpdateCheckCompleted -= OnUpdateCheckCompleted;
                UpdateChecker.OnUpdateCheckCompleted += OnUpdateCheckCompleted;
                UpdateChecker.Instance.CheckForUpdate();
            }

            ModelListManager.OnModelChanged -= OnModelChanged;
            ModelListManager.OnModelChanged += OnModelChanged;

            ModLogger.Log("ConfigWindow initialized.");
        }

        private void CreateOrFindUiRoot()
        {
            var existing = GameObject.Find("DuckovCustomModelCanvas");
            if (existing != null)
            {
                _uiRoot = existing;
                return;
            }

            var canvas = new GameObject("DuckovCustomModelCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 9999;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _uiRoot = canvas;
            DontDestroyOnLoad(_uiRoot);
        }

        private void BuildMainPanel()
        {
            if (_uiRoot == null) return;

            _overlay = UIFactory.CreateImage("Overlay", _uiRoot.transform, new Color(0, 0, 0, 0.5f));
            UIFactory.SetupRectTransform(_overlay, Vector2.zero, Vector2.one, Vector2.zero);

            _panelRoot = UIFactory.CreateImage("MainPanel", _uiRoot.transform, new Color(0.1f, 0.12f, 0.15f, 0.95f));
            UIFactory.SetupRectTransform(_panelRoot, new(0.1f, 0.1f), new(0.9f, 0.9f), Vector2.zero,
                anchoredPosition: Vector2.zero);

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            BuildTitleBar();
        }

        private void BuildTitleBar()
        {
            if (_panelRoot == null) return;

            var titleBar =
                UIFactory.CreateImage("TitleBar", _panelRoot.transform, new Color(0.15f, 0.18f, 0.22f, 0.9f));
            UIFactory.SetupRectTransform(titleBar, new(0, 1), new(1, 1), new Vector2(0, 40),
                pivot: new Vector2(0.5f, 1), anchoredPosition: new Vector2(0, 0));

            var titleContainer = new GameObject("TitleContainer", typeof(RectTransform));
            titleContainer.transform.SetParent(titleBar.transform, false);
            UIFactory.SetupRectTransform(titleContainer, new(0, 0), new(1, 1),
                offsetMin: new Vector2(10, 0),
                offsetMax: new Vector2(-10, 0));
            UIFactory.SetupHorizontalLayoutGroup(titleContainer, 12f, new(0, 0, 0, 0), TextAnchor.MiddleCenter,
                true, false, false, false);

            var titleText = UIFactory.CreateText("Title", titleContainer.transform, Localization.Title,
                20,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetLocalizedText(titleText, () => Localization.Title);

            UIFactory.SetupRectTransform(titleText, Vector2.zero, Vector2.zero, new Vector2(0, 0));
            UIFactory.SetupContentSizeFitter(titleText);

            var versionLabel = UIFactory.CreateText("Version", titleContainer.transform, $"v{Constant.ModVersion}", 14,
                new Color(0.8f, 0.8f, 0.8f, 1));
            UIFactory.SetupRectTransform(versionLabel, Vector2.zero, Vector2.zero, new Vector2(0, 0));
            UIFactory.SetupContentSizeFitter(versionLabel);

            _updateIndicatorTitle = UIFactory.CreateText("UpdateIndicator", titleContainer.transform, "", 16,
                new Color(1f, 0.6f, 0f, 1), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetupRectTransform(_updateIndicatorTitle, Vector2.zero, Vector2.zero, new Vector2(0, 0));
            UIFactory.SetupContentSizeFitter(_updateIndicatorTitle);
            _updateIndicatorTitle.SetActive(false);

            var titleContainerRect = titleContainer.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(titleContainerRect);

            var closeButton = UIFactory.CreateButton("CloseButton", titleBar.transform, HidePanel,
                new Color(0.2f, 0.2f, 0.2f, 1));
            UIFactory.SetupRectTransform(closeButton, new(1, 0.5f), new(1, 0.5f), new Vector2(36, 36),
                pivot: new Vector2(1, 0.5f), anchoredPosition: new Vector2(-10, 0));

            var closeText = UIFactory.CreateText("Text", closeButton.transform, "×", 24, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(closeText, Vector2.zero, Vector2.one, Vector2.zero);
        }

        private void SetupPanels()
        {
            if (_panelRoot == null) return;

            CreateTabPanels();
            SetupTabSystem();
        }

        private void CreateTabPanels()
        {
            if (_panelRoot == null) return;

            var modelTabContainer = new GameObject("ModelTabContainer", typeof(RectTransform));
            modelTabContainer.transform.SetParent(_panelRoot.transform, false);
            UIFactory.SetupRectTransform(modelTabContainer, new(0, 0), new(1, 1),
                offsetMin: new Vector2(10, 10),
                offsetMax: new Vector2(-10, -100));

            _modelSelectionTab = modelTabContainer.AddComponent<ModelSelectionTab>();

            var settingsTabContainer = new GameObject("SettingsTabContainer", typeof(RectTransform));
            settingsTabContainer.transform.SetParent(_panelRoot.transform, false);
            UIFactory.SetupRectTransform(settingsTabContainer, new(0, 0), new(1, 1),
                offsetMin: new Vector2(10, 10),
                offsetMax: new Vector2(-10, -100));

            _settingsTab = settingsTabContainer.AddComponent<SettingsTab>();
            _settingsTab.OnAnimatorParamsToggleChanged += SetAnimatorParamsWindowVisible;
        }

        private void SetupTabSystem()
        {
            if (_panelRoot == null || _modelSelectionTab == null || _settingsTab == null) return;

            _tabSystem = gameObject.AddComponent<TabSystem>();
            _tabSystem.Initialize(_panelRoot.transform);

            _tabSystem.AddTab(Localization.ModelSelection, _modelSelectionTab.gameObject, true, "ModelSelection");
            _tabSystem.AddTab(Localization.Settings, _settingsTab.gameObject, false, "Settings");
        }

        private static bool IsTypingInInputField()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null) return false;

            var inputField = current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            return inputField != null && inputField.isFocused;
        }

        public void ShowPanel(int tabIndex = 0)
        {
            if (_panelRoot != null && _panelRoot.activeSelf)
            {
                _tabSystem?.SwitchToTab(tabIndex);
                return;
            }

            ShowPanelInternal(tabIndex);
        }

        private void ShowPanelInternal(int tabIndex)
        {
            if (!_isInitialized || _panelRoot == null)
            {
                ModLogger.LogWarning("Cannot show panel - not initialized!");
                return;
            }

            _uiActive = true;
            if (_overlay != null) _overlay.SetActive(true);
            if (_panelRoot != null) _panelRoot.SetActive(true);

            _modelSelectionTab?.Initialize();
            _settingsTab?.Initialize();

            _tabSystem?.SwitchToTab(tabIndex);

            _cursorWasVisible = Cursor.visible;
            _originalCursorLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var current = EventSystem.current;
            if (current == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            _charInput = CharacterInputControl;
            if (_charInput != null)
            {
                _charInputWasEnabled = _charInput.enabled;
                if (_charInputWasEnabled)
                {
                    _charInput.enabled = false;
                    ModLogger.Log("CharacterInputControl disabled.");
                }
            }

            _playerInput = PlayerInput;
            if (_playerInput != null)
            {
                _playerInputWasActive = _playerInput.inputIsActive;
                if (_playerInputWasActive)
                {
                    _playerInput.DeactivateInput();
                    ModLogger.Log("PlayerInput deactivated (game input blocked).");
                }
            }

            StartCoroutine(ForceCursorFree());
            ModLogger.Log("Config window opened.");
        }

        public void HidePanel()
        {
            _uiActive = false;
            StopAllCoroutines();

            if (_overlay != null) _overlay.SetActive(false);
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_charInput != null && _charInputWasEnabled)
            {
                _charInput.enabled = true;
                ModLogger.Log("CharacterInputControl re-enabled.");
            }

            _charInput = null;
            _charInputWasEnabled = false;

            if (_playerInput != null && _playerInputWasActive)
            {
                _playerInput.ActivateInput();
                ModLogger.Log("PlayerInput reactivated (game input restored).");
            }

            _playerInput = null;
            _playerInputWasActive = false;

            Cursor.visible = _cursorWasVisible;
            Cursor.lockState = _originalCursorLockState;
            ModLogger.Log("Config window closed.");
        }

        private IEnumerator ForceCursorFree()
        {
            while (_uiActive)
            {
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible)
                    Cursor.visible = true;
                yield return null;
            }
        }

        private void BuildAnimatorParamsPanel()
        {
            if (_uiRoot == null) return;

            var panelObj = new GameObject("AnimatorParamsPanel", typeof(RectTransform), typeof(AnimatorParamsPanel));
            panelObj.transform.SetParent(_uiRoot.transform, false);
            _animatorParamsPanel = panelObj.GetComponent<AnimatorParamsPanel>();
            _animatorParamsPanel.Initialize(_uiRoot.transform);
            _animatorParamsPanel.Hide();
        }

        public void SetAnimatorParamsWindowVisible(bool visible)
        {
            _showAnimatorParamsWindow = visible;
            if (_animatorParamsPanel == null) return;
            if (visible)
                _animatorParamsPanel.Show();
            else
                _animatorParamsPanel.Hide();
        }

        public void OnAnimatorParamsPanelOpened()
        {
            if (_showAnimatorParamsWindow) return;
            _showAnimatorParamsWindow = true;
            _settingsTab?.RefreshAnimatorParamsToggleState(true);
        }

        public void OnAnimatorParamsPanelClosed()
        {
            if (!_showAnimatorParamsWindow) return;
            _showAnimatorParamsWindow = false;
            _settingsTab?.RefreshAnimatorParamsToggleState(false);
        }

        public bool IsAnimatorParamsWindowVisible()
        {
            return _animatorParamsPanel != null && _animatorParamsPanel.IsVisible();
        }

        private void BuildSettingsButton()
        {
            if (_uiRoot == null) return;

            var uiConfig = UIConfig;
            if (uiConfig == null) return;

            _settingsButton = UIFactory.CreateButton("SettingsButton", _uiRoot.transform, OnSettingsButtonClicked,
                new Color(0.2f, 0.25f, 0.3f, 0.9f));

            var anchorMin = GetAnchorValue(uiConfig.DCMButtonAnchor);
            UIFactory.SetupRectTransform(_settingsButton, anchorMin, anchorMin, new Vector2(80, 50), pivot: anchorMin,
                anchoredPosition: new Vector2(uiConfig.DCMButtonOffsetX, uiConfig.DCMButtonOffsetY));

            var outline = _settingsButton.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var buttonText = UIFactory.CreateText("Text", _settingsButton.transform, "DCM", 24, Color.white,
                TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(buttonText, 16, 24);

            _updateIndicatorButton = UIFactory.CreateText("UpdateIndicator", _settingsButton.transform, "!", 16,
                new Color(1f, 0.6f, 0f, 1), TextAnchor.MiddleCenter);
            UIFactory.SetupRectTransform(_updateIndicatorButton, new(0.8f, 0.8f), new(1f, 1f),
                new Vector2(0, 0),
                pivot: new Vector2(1f, 1f), anchoredPosition: new Vector2(-2, -2));
            UIFactory.SetupContentSizeFitter(_updateIndicatorButton);
            _updateIndicatorButton.SetActive(false);

            UIFactory.SetupButtonColors(_settingsButton.GetComponent<Button>(),
                new Color(0.2f, 0.25f, 0.3f, 0.9f),
                new Color(0.3f, 0.35f, 0.4f, 0.9f),
                new Color(0.15f, 0.2f, 0.25f, 0.9f));

            SetSettingsButtonVisible(false);
        }

        private void OnSettingsButtonClicked()
        {
            if (!_isInitialized)
            {
                ModLogger.LogWarning("Cannot toggle panel - not initialized!");
                return;
            }

            if (_panelRoot == null)
            {
                ModLogger.LogWarning("Cannot toggle panel - panel root is null!");
                return;
            }

            if (_panelRoot.activeSelf)
                HidePanel();
            else
                ShowPanel();
        }

        private void SetSettingsButtonVisible(bool visible)
        {
            if (_settingsButton != null) _settingsButton.SetActive(visible);
        }

        public void RefreshSettingsButton()
        {
            if (_settingsButton == null || UIConfig == null) return;

            var anchorMin = GetAnchorValue(UIConfig.DCMButtonAnchor);
            UIFactory.SetupRectTransform(_settingsButton, anchorMin, anchorMin, new Vector2(80, 50), pivot: anchorMin,
                anchoredPosition: new Vector2(UIConfig.DCMButtonOffsetX, UIConfig.DCMButtonOffsetY));

            var buttonRect = _settingsButton.GetComponent<RectTransform>();
            if (buttonRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);
        }

        private void UpdateSettingsButtonVisibility()
        {
            var uiConfig = UIConfig;
            if (uiConfig == null)
            {
                SetSettingsButtonVisible(false);
                return;
            }

            var shouldShow = uiConfig.ShowDCMButton && (IsInMainMenu() || IsInLootView());
            SetSettingsButtonVisible(shouldShow);
        }

        private static bool IsInMainMenu()
        {
            var currentScene = SceneManager.GetActiveScene();
            return currentScene.name == "MainMenu";
        }

        private static bool IsInLootView()
        {
            var activeView = View.ActiveView;
            var lootView = LootView.Instance;
            return activeView != null && lootView != null && activeView == lootView;
        }

        private static Vector2 GetAnchorValue(AnchorPosition position)
        {
            return position switch
            {
                AnchorPosition.TopLeft => new(0f, 1f),
                AnchorPosition.TopCenter => new(0.5f, 1f),
                AnchorPosition.TopRight => new(1f, 1f),
                AnchorPosition.MiddleLeft => new(0f, 0.5f),
                AnchorPosition.MiddleCenter => new(0.5f, 0.5f),
                AnchorPosition.MiddleRight => new(1f, 0.5f),
                AnchorPosition.BottomLeft => new(0f, 0f),
                AnchorPosition.BottomCenter => new(0.5f, 0f),
                AnchorPosition.BottomRight => new(1f, 0f),
                _ => new(0f, 1f),
            };
        }

        private void OnUpdateCheckCompleted(bool hasUpdate, string? latestVersion)
        {
            UpdateUpdateIndicators();
        }

        private void UpdateUpdateIndicators()
        {
            var updateChecker = UpdateChecker.Instance;
            if (updateChecker == null) return;

            var hasUpdate = updateChecker.HasUpdate();
            var latestVersion = updateChecker.GetLatestVersion();

            if (_updateIndicatorTitle != null)
            {
                if (hasUpdate && !string.IsNullOrEmpty(latestVersion))
                {
                    _updateIndicatorTitle.SetActive(true);
                    var localizedText = _updateIndicatorTitle.GetComponent<LocalizedText>();
                    if (localizedText != null)
                    {
                        localizedText.SetTextGetter(() => $"{Localization.UpdateAvailable}: v{latestVersion}");
                    }
                    else
                    {
                        var text = _updateIndicatorTitle.GetComponent<TextMeshProUGUI>();
                        if (text != null)
                            text.text = $"{Localization.UpdateAvailable}: v{latestVersion}";
                    }
                }
                else
                {
                    _updateIndicatorTitle.SetActive(false);
                }
            }

            if (_updateIndicatorButton != null) _updateIndicatorButton.SetActive(hasUpdate);
        }

        private void HandleShortcutParameters(UIConfig? uiConfig)
        {
            if (uiConfig == null) return;

            var modifier1Pressed = InputBlocker.GetRealKey(uiConfig.EmotionModifierKey1);
            var modifier2Pressed = InputBlocker.GetRealKey(uiConfig.EmotionModifierKey2);
            var anyModifierPressed = modifier1Pressed || modifier2Pressed;

            switch (anyModifierPressed)
            {
                case true when !_emotionModifierBlockingInput:
                    InputBlocker.BlockInput();
                    _emotionModifierBlockingInput = true;
                    break;
                case false when _emotionModifierBlockingInput:
                    InputBlocker.UnblockInput();
                    _emotionModifierBlockingInput = false;
                    break;
            }

            if (!anyModifierPressed) return;

            for (var i = 0; i < 8; i++)
            {
                var fKey = KeyCode.F1 + i;
                if (!InputBlocker.GetRealKeyDown(fKey)) continue;

                var value1Changed = false;
                var value2Changed = false;

                if (modifier1Pressed)
                {
                    EmotionParameterValue1 = i;
                    value1Changed = true;
                }

                if (modifier2Pressed)
                {
                    EmotionParameterValue2 = i;
                    value2Changed = true;
                }

                if (value1Changed || value2Changed) UpdateAnimatorEmotionValues();
            }
        }

        private void UpdateAnimatorEmotionValues()
        {
            EmotionParameterManager.NotifyEmotionParametersChanged(EmotionParameterValue1, EmotionParameterValue2);
        }

        private void OnModelChanged(ModelChangedEventArgs? e)
        {
            if (e is not { TargetTypeId: ModelTargetType.Character, Handler: ModelHandler handler }) return;
            if (handler.CharacterMainControl != CharacterMainControl.Main) return;
            EmotionParameterManager.NotifyEmotionParametersChanged(EmotionParameterValue1, EmotionParameterValue2);
        }
    }
}
