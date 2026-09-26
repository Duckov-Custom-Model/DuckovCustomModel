using System;
using System.Collections.Generic;
using Duckov.UI;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Integrations.Ysm;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.UI.Base;
using DuckovCustomModel.UI.Utils;
using ModelRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckovCustomModel.UI
{
    [DefaultExecutionOrder(-10000)]
    public sealed class YsmRadialMenu : MonoBehaviour
    {
        private static readonly Color IdleColor = new(0, 0, 0, 0x90 / 255f);
        private static readonly Color SelectedColor = new(1, 0xB1 / 255f, 0, 0xF0 / 255f);
        private static readonly Color ConfigColor = new(0, 0xCE / 255f, 1, 0x70 / 255f);
        private static readonly Color ConfigSelectedColor = new(0, 0xCE / 255f, 1, 0xF0 / 255f);
        private readonly RadialMenuController _assetState = new();
        private readonly YsmRadialMenuGraphic[] _configSectors = new YsmRadialMenuGraphic[8];
        private readonly Dictionary<string, double> _formDefaults = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _formValues = new(StringComparer.Ordinal);
        private readonly TextMeshProUGUI[] _gears = new TextMeshProUGUI[8];
        private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[8];
        private readonly YsmRadialMenuGraphic[] _sectors = new YsmRadialMenuGraphic[8];
        private readonly YsmRadialMenuController _state = new();
        private InputBlocker? _blocker;
        private CharacterInputControl? _characterInput;
        private bool _characterWasEnabled;
        private CursorLockMode _cursorLock;
        private bool _cursorWasVisible;
        private RectTransform? _formContent;
        private ScrollRect? _formScroll;
        private ModelHandler? _handler;
        private TextMeshProUGUI? _hintText;
        private TextMeshProUGUI? _lockText;
        private Button? _next;
        private string _openedModelId = string.Empty;
        private string _shownAssetActionId = string.Empty;
        private ModelInfo? _openedModelInfo;
        private GameObject? _ownedEventSystem;
        private bool _ownsInput;
        private TextMeshProUGUI? _pageText;
        private TextMeshProUGUI? _pathText;
        private Button? _previous;
        private int _releaseAfterFrame = -1;
        private GameObject? _root;
        private YsmCharacterRuntime? _runtime;
        private float _saveFormsAt = -1;
        private string _settingsModelId = string.Empty;
        private YsmCharacterRuntime? _settingsRuntime;
        private bool _showingForms;
        private RectTransform? _wheel;

        public static YsmRadialMenu? Instance { get; private set; }
        public bool IsOpen => _state.IsOpen || _assetState.IsOpen;
        private bool IsAssetMenu => _assetState.IsOpen;
        private int VisibleCount => IsAssetMenu ? _assetState.VisibleCount : _state.VisibleCount;
        private static KeyCode OpenKey => ModEntry.UIConfig?.YsmRadialMenuKey ?? KeyCode.Z;
        private static bool OtherUiOpen => View.ActiveView != null || ConfigWindow.Instance?.IsOpen == true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (_saveFormsAt >= 0 && Time.unscaledTime >= _saveFormsAt) SaveForms();
            if (_releaseAfterFrame >= 0 && Time.frameCount > _releaseAfterFrame) ReleaseInput();
            if (!IsOpen)
            {
                if (!_ownsInput && OpenKey != KeyCode.None && InputBlocker.GetRealKeyDown(OpenKey)) Show();
                return;
            }

            if (_handler == null || !ReferenceEquals(_handler, CurrentHandler()) ||
                !ReferenceEquals(_openedModelInfo, _handler.CurrentModelInfo) ||
                (_runtime != null && (!_runtime.IsAvailable || !ReferenceEquals(_runtime, _handler.YsmRuntime))) ||
                (IsAssetMenu && (_handler.YsmRuntime != null || _handler.RadialMenuEntries.Count == 0)) || OtherUiOpen)
            {
                HideImmediately();
                return;
            }

            KeepInputBlocked();
            if (InputBlocker.GetRealKeyDown(KeyCode.Escape) ||
                (OpenKey != KeyCode.None && InputBlocker.GetRealKeyDown(OpenKey)))
            {
                Hide();
                return;
            }

            var scroll = InputCompatibility.MouseScrollY;
            if (scroll != 0)
                if (_wheel != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _wheel, InputCompatibility.MousePosition, null, out var scrollMouse))
                {
                    if (scrollMouse.x < 220) ChangePage(scroll < 0 ? 1 : -1);
                    else if (_showingForms && _formScroll != null)
                        _formScroll.verticalNormalizedPosition = Mathf.Clamp01(
                            _formScroll.verticalNormalizedPosition + (scroll > 0 ? .12f : -.12f));
                }

            if (_wheel == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_wheel, InputCompatibility.MousePosition, null,
                out var mouse);
            var slot = IsAssetMenu
                ? _assetState.HitTest(mouse.x / 2, -mouse.y / 2)
                : _state.HitTest(mouse.x / 2, -mouse.y / 2);
            var configSlot = IsAssetMenu
                ? _assetState.HitTest(mouse.x / 2, -mouse.y / 2, true)
                : _state.HitTest(mouse.x / 2, -mouse.y / 2, true);
            for (var i = 0; i < VisibleCount; i++)
            {
                var selected = i == slot;
                var hasConfig = IsAssetMenu
                    ? _assetState.GetEntry(i).HasConfiguration
                    : _state.GetBinding(i).Button != null;
                SetRadii(_sectors[i], selected && hasConfig ? 100 : 50, selected ? 230 : 210);
                _sectors[i].color = selected ? SelectedColor : IdleColor;
                SetRadii(_configSectors[i], i == configSlot ? 30 : 50, 100);
                _configSectors[i].color = i == configSlot ? ConfigSelectedColor : ConfigColor;
            }

            if (!InputCompatibility.GetMouseButtonDown(0) && !InputCompatibility.GetMouseButtonDown(1) &&
                !InputCompatibility.GetMouseButtonDown(2)) return;
            if (mouse.sqrMagnitude <= 40f * 40f)
            {
                if (IsAssetMenu) ApplySelection(_assetState.Stop());
                else if (_runtime != null) ApplySelection(_state.ToggleMovementLock(_runtime.ExtraAnimationLocked));
                return;
            }

            if (IsAssetMenu) ApplySelection(_assetState.SelectAt(mouse.x / 2, -mouse.y / 2));
            else ApplySelection(_state.SelectAt(mouse.x / 2, -mouse.y / 2));
        }

        private void LateUpdate()
        {
            RestoreCurrentRuntime();
            if (IsOpen) KeepInputBlocked();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            HideImmediately();
            SaveForms();
        }

        private void OnDestroy()
        {
            HideImmediately();
            SaveForms();
            if (_root != null) Destroy(_root);
            if (_ownedEventSystem != null) Destroy(_ownedEventSystem);
            if (Instance == this) Instance = null;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) HideImmediately();
        }

        private void SceneChanged(Scene previous, Scene next)
        {
            HideImmediately();
            if (_ownedEventSystem != null) Destroy(_ownedEventSystem);
            _ownedEventSystem = null;
        }

        private static void SetRadii(YsmRadialMenuGraphic graphic, float inner, float outer)
        {
            if (graphic.InnerRadius == inner && graphic.OuterRadius == outer) return;
            graphic.InnerRadius = inner;
            graphic.OuterRadius = outer;
            graphic.SetVerticesDirty();
        }

        private static ModelHandler? CurrentHandler()
        {
            var player = CharacterMainControl.Main;
            return player == null ? null : player.GetComponent<ModelHandler>();
        }

        private static YsmCharacterRuntime? CurrentRuntime()
        {
            return CurrentHandler()?.YsmRuntime;
        }

        public bool Show()
        {
            if (!isActiveAndEnabled || IsOpen || _ownsInput || OtherUiOpen || IsTyping()) return false;
            var handler = CurrentHandler();
            var runtime = handler?.YsmRuntime;
            if (handler == null || (runtime == null && handler.RadialMenuEntries.Count == 0) ||
                (runtime != null && !runtime.IsAvailable)) return false;
            var input = GameManager.MainPlayerInput;
            if (input == null || !input.inputIsActive || input.actions == null || !input.actions.enabled) return false;
            var blocker = InputBlocker.Instance;
            if (blocker == null || blocker.IsBlocked || blocker.IsExternalBlocking) return false;
            RestoreCurrentRuntime();
            _handler = handler;
            _runtime = runtime;
            _openedModelInfo = handler.CurrentModelInfo;
            _openedModelId = handler.CurrentModelInfo?.ModelID ?? string.Empty;
            bool opened;
            if (runtime != null)
            {
                opened = _state.Open(runtime.Presentation, _openedModelId);
            }
            else
            {
                opened = _assetState.Open(handler.RadialMenuEntries,
                    handler.CurrentModelInfo?.Name ?? "模型", _openedModelId);
            }

            if (!opened)
            {
                _runtime = null;
                _handler = null;
                _openedModelInfo = null;
                return false;
            }

            EnsureUi();
            EnsureEventSystem();
            CaptureInput(blocker);
            _root!.SetActive(true);
            ClearForms();
            Refresh();
            KeepInputBlocked();
            return true;
        }

        public void Hide()
        {
            SaveForms();
            _handler?.SaveRadialForms();
            _state.Close();
            _assetState.Close();
            if (_root != null) _root.SetActive(false);
            ClearForms();
            _showingForms = false;
            _shownAssetActionId = string.Empty;
            if (_ownsInput) _releaseAfterFrame = Time.frameCount;
            _runtime = null;
            _handler = null;
            _openedModelInfo = null;
            _openedModelId = string.Empty;
        }

        public void HideImmediately()
        {
            Hide();
            ReleaseInput();
        }

        private static bool IsTyping()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && (selected.GetComponent<TMP_InputField>() != null ||
                                        selected.GetComponent<InputField>() != null);
        }

        private void CaptureInput(InputBlocker blocker)
        {
            _blocker = blocker;
            _characterInput = CharacterInputControl.Instance;
            _characterWasEnabled = _characterInput != null && _characterInput.enabled;
            _cursorWasVisible = Cursor.visible;
            _cursorLock = Cursor.lockState;
            _ownsInput = true;
            _releaseAfterFrame = -1;
            var uiModule = EventSystem.current?.currentInputModule as InputSystemUIInputModule
                           ?? EventSystem.current?.GetComponent<InputSystemUIInputModule>();
            blocker.BeginModalUi(uiModule);
        }

        private void KeepInputBlocked()
        {
            if (_characterInput != null) _characterInput.enabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void ReleaseInput()
        {
            _releaseAfterFrame = -1;
            if (!_ownsInput) return;
            _ownsInput = false;
            var anotherUi = OtherUiOpen;
            _blocker?.EndModalUi();
            if (_characterInput != null && _characterInput == CharacterInputControl.Instance)
                _characterInput.enabled = _characterWasEnabled;
            if (!anotherUi)
            {
                Cursor.visible = _cursorWasVisible;
                Cursor.lockState = _cursorLock;
            }

            _characterInput = null;
            _blocker = null;
        }

        private void ApplySelection(YsmRadialMenuSelection selection)
        {
            switch (selection.Kind)
            {
                case YsmRadialMenuSelectionKind.Animation:
                    _runtime?.PlayExtra(selection.AnimationId);
                    Hide();
                    break;
                case YsmRadialMenuSelectionKind.Stop:
                    _runtime?.StopExtra();
                    Hide();
                    break;
                case YsmRadialMenuSelectionKind.Closed:
                    Hide();
                    break;
                case YsmRadialMenuSelectionKind.Classification:
                case YsmRadialMenuSelectionKind.Returned:
                    _showingForms = false;
                    Refresh();
                    break;
                case YsmRadialMenuSelectionKind.Configuration:
                    if (selection.Button != null) ShowForms(selection.Button);
                    break;
                case YsmRadialMenuSelectionKind.MovementLock:
                    if (_runtime != null) _runtime.ExtraAnimationLocked = selection.MovementLocked;
                    if (_lockText != null) _lockText.text = selection.MovementLocked ? "锁定：开" : "锁定：关";
                    break;
            }
        }

        private void ApplySelection(RadialMenuSelection selection)
        {
            switch (selection.Kind)
            {
                case RadialMenuSelectionKind.Action:
                    _handler?.TryPlayRadialAction(selection.ActionId);
                    Hide();
                    break;
                case RadialMenuSelectionKind.Stop:
                    _handler?.StopRadialAction();
                    Hide();
                    break;
                case RadialMenuSelectionKind.Closed:
                    Hide();
                    break;
                case RadialMenuSelectionKind.Classification:
                case RadialMenuSelectionKind.Returned:
                    _showingForms = false;
                    Refresh();
                    break;
                case RadialMenuSelectionKind.Configuration:
                    ShowAssetForms(selection.ActionId);
                    break;
            }
        }

        private void Back()
        {
            if (IsAssetMenu) ApplySelection(_assetState.Back());
            else ApplySelection(_state.Back());
        }

        private void ChangePage(int direction)
        {
            if (IsAssetMenu) _assetState.ChangePage(direction);
            else _state.ChangePage(direction);
            Refresh();
        }

        private void Refresh()
        {
            var pageIndex = IsAssetMenu ? _assetState.PageIndex : _state.PageIndex;
            var pageCount = IsAssetMenu ? _assetState.PageCount : _state.PageCount;
            _pathText!.text = MinecraftFormatting.ToTmp(IsAssetMenu ? _assetState.PathLabel : _state.PathLabel);
            _pageText!.text = $"{pageIndex + 1} / {pageCount}";
            _previous!.interactable = pageIndex > 0;
            _next!.interactable = pageIndex + 1 < pageCount;
            _hintText!.text = IsAssetMenu
                ? $"{OpenKey} / Esc 关闭 · 点击选择 · 左侧滚轮翻页 · 配置区滚轮滚动 · 中心停止动作"
                : $"{OpenKey} / Esc 关闭 · 点击选择 · 左侧滚轮翻页 · 配置区滚轮滚动";
            if (_lockText != null)
                _lockText.text = IsAssetMenu ? "停止动作" :
                    _runtime?.ExtraAnimationLocked == true ? "锁定：开" : "锁定：关";
            if (_formScroll != null) _formScroll.gameObject.SetActive(true);
            for (var i = 0; i < 8; i++)
            {
                var visible = i < VisibleCount;
                _sectors[i].gameObject.SetActive(visible);
                _labels[i].gameObject.SetActive(visible);
                var binding = visible && !IsAssetMenu ? _state.GetBinding(i) : default;
                var assetEntry = visible && IsAssetMenu ? _assetState.GetEntry(i) : null;
                var hasConfig = visible && (IsAssetMenu
                    ? assetEntry?.HasConfiguration == true
                    : binding.Button != null);
                _configSectors[i].gameObject.SetActive(hasConfig);
                _gears[i].gameObject.SetActive(hasConfig);
                if (!visible) continue;
                var displayName = assetEntry?.Name ?? binding.DisplayName;
                _labels[i].text = MinecraftFormatting.ToTmp(string.IsNullOrWhiteSpace(displayName)
                    ? (pageIndex * 8 + i).ToString()
                    : displayName);
                _labels[i].color = assetEntry?.Kind == RadialMenuEntryKind.Classification ||
                                   binding.Kind == YsmExtraAnimationKind.Classification
                    ? new(1, 0.65f, 0.55f)
                    : assetEntry != null && string.Equals(assetEntry.Id, _handler?.CurrentRadialActionId,
                        StringComparison.Ordinal)
                        ? SelectedColor
                        : Color.white;
            }

            if (!_showingForms) ClearForms();
        }

        private void EnsureUi()
        {
            if (_root != null) return;
            _root = new("YsmRadialMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1280, 720);
            scaler.matchWidthOrHeight = 1;
            var overlay = UIFactory.CreateImage("Backdrop", _root.transform, new Color(0, 0, 0, 0.45f));
            var backdrop = overlay.GetComponent<RectTransform>();
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            _wheel = Rect("Wheel", _root.transform, new(-145, 0), new(440, 440));
            _pathText = Text("Path", _root.transform, "YSM", new(0, 270), new(900, 38), 23);
            _pathText.richText = true;
            for (var i = 0; i < 8; i++)
            {
                var sector = Rect("Action" + i, _wheel, Vector2.zero, new(440, 440)).gameObject
                    .AddComponent<YsmRadialMenuGraphic>();
                sector.Slot = i;
                sector.InnerRadius = 50;
                sector.OuterRadius = 210;
                sector.color = IdleColor;
                sector.raycastTarget = false;
                _sectors[i] = sector;
                var config = Rect("Config" + i, _wheel, Vector2.zero, new(440, 440)).gameObject
                    .AddComponent<YsmRadialMenuGraphic>();
                config.Slot = i;
                config.InnerRadius = 50;
                config.OuterRadius = 100;
                config.raycastTarget = false;
                _configSectors[i] = config;
                var angle = (22.5f + i * 45) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
                _labels[i] = Text("Label" + i, _wheel, "", direction * 155, new(105, 64), 16);
                _labels[i].richText = true;
                _labels[i].enableAutoSizing = true;
                _labels[i].fontSizeMin = 11;
                _labels[i].fontSizeMax = 16;
                _gears[i] = Text("Settings" + i, _wheel, "⚙", direction * 72, new(30, 30), 18);
            }

            var lockBackground =
                UIFactory.CreateImage("MovementLock", _root.transform, new Color(0.15f, 0.18f, 0.22f, 0.98f));
            var lockRect = lockBackground.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new(0.5f, 0.5f);
            lockRect.sizeDelta = new(80, 40);
            lockRect.anchoredPosition = new(-145, 0);
            lockBackground.GetComponent<Image>().raycastTarget = false;
            _lockText = Text("Label", lockBackground.transform, "锁定：关", Vector2.zero, new(74, 38), 16);
            _previous = Button("Previous", "<", new(145, 184), new(48, 42), () => ChangePage(-1));
            _next = Button("Next", ">", new(405, 184), new(48, 42), () => ChangePage(1));
            _pageText = Text("Page", _root.transform, "", new(275, 184), new(190, 42), 21);
            Button("Back", "返回", new(275, 132), new(308, 40), Back);
            _hintText = Text("Hint", _root.transform, "", new(0, -285), new(1050, 40), 17);
            var forms = UIFactory.CreateScrollView("Configuration", _root.transform, out var content);
            _formScroll = forms;
            var formsRect = forms.GetComponent<RectTransform>();
            formsRect.anchorMin = formsRect.anchorMax = new(0.5f, 0.5f);
            formsRect.sizeDelta = new(308, 296);
            formsRect.anchoredPosition = new(275, -36);
            forms.scrollSensitivity = 0; // The raw scroll route separates page and configuration scrolling.
            var scrollbar = UIFactory.CreateScrollbar(forms, 8, true);
            scrollbar.transform.SetParent(forms.transform, false);
            _formContent = content.GetComponent<RectTransform>();
            var layout = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3;
            layout.padding = new(8, 12, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _root.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current == null && _ownedEventSystem == null)
            {
                _ownedEventSystem = new("YsmRadialMenuEventSystem", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                var module = _ownedEventSystem.GetComponent<InputSystemUIInputModule>();
                if (module.actionsAsset == null) module.AssignDefaultActions();
                _ownedEventSystem.transform.SetParent(transform, false);
            }
        }

        private Button Button(string name, string label, Vector2 position, Vector2 size, UnityAction action)
        {
            var obj = UIFactory.CreateButton(name, _root!.transform, action, new Color(0.15f, 0.18f, 0.22f, 0.98f));
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var text = UIFactory.CreateText("Label", obj.transform, label, 17, alignment: TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(text);
            text.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            return obj.GetComponent<Button>();
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, Vector2 position, Vector2 size,
            int fontSize)
        {
            var obj = UIFactory.CreateText(name, parent, value, fontSize, alignment: TextAnchor.MiddleCenter);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        private void ClearForms()
        {
            if (_formContent == null) return;
            foreach (Transform child in _formContent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void FormLabel(string text, int size = 15)
        {
            var obj = UIFactory.CreateText("Description", _formContent!, text, size);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.richText = true;
            label.text = MinecraftFormatting.ToTmp(text);
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Truncate;
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = 30;
            layout.preferredHeight = Math.Max(30, label.GetPreferredValues(label.text, 270, 0).y + 6);
        }

        private void ShowForms(YsmExtraAnimationButton button, bool preservePosition = false)
        {
            if (_runtime == null) return;
            var scrollPosition = preservePosition && _formScroll != null
                ? _formScroll.verticalNormalizedPosition
                : 1f;
            ClearForms();
            _showingForms = true;
            for (var index = 0; index < button.Forms.Count; index++)
            {
                var form = button.Forms[index];
                FormLabel(form.Title);
                if (!string.IsNullOrWhiteSpace(form.Description)) FormLabel(form.Description, 12);
                try
                {
                    CreateForm(form, button, index);
                }
                catch (Exception exception)
                {
                    ModLogger.LogWarning($"YSM form '{form.Title}': {exception.Message}");
                    FormLabel("此配置不可用", 12);
                }
            }

            if (button.Forms.Count > 0)
            {
                var reset = UIFactory.CreateButton("ResetForms", _formContent!, () =>
                {
                    ResetForms();
                    ShowForms(button, true);
                }, new Color(0.3f, 0.35f, 0.4f));
                reset.AddComponent<LayoutElement>().preferredHeight = 32;
                var text = UIFactory.CreateText("Label", reset.transform, "还原此模型配置",
                    alignment: TextAnchor.MiddleCenter);
                UIFactory.SetupButtonText(text);
            }

            if (_formScroll != null && _formContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_formContent);
                _formScroll.StopMovement();
                _formScroll.verticalNormalizedPosition = scrollPosition;
            }
        }

        private void ShowAssetForms(string actionId, bool preservePosition = false)
        {
            if (_handler == null || !IsAssetMenu) return;
            var forms = _handler.GetRadialActionForms(actionId);
            if (forms.Count == 0) return;
            var scrollPosition = preservePosition && _formScroll != null
                ? _formScroll.verticalNormalizedPosition
                : 1f;
            ClearForms();
            _showingForms = true;
            _shownAssetActionId = actionId;
            foreach (var form in forms)
            {
                FormLabel(form.Name);
                if (!string.IsNullOrWhiteSpace(form.Description)) FormLabel(form.Description, 12);
                CreateAssetForm(actionId, form);
            }

            var reset = UIFactory.CreateButton("ResetForms", _formContent!, () =>
            {
                _handler?.ResetRadialForms();
                ShowAssetForms(actionId, true);
            }, new Color(0.3f, 0.35f, 0.4f));
            reset.AddComponent<LayoutElement>().preferredHeight = 32;
            var text = UIFactory.CreateText("Label", reset.transform, "还原此模型配置",
                alignment: TextAnchor.MiddleCenter);
            UIFactory.SetupButtonText(text);
            if (_formScroll != null && _formContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_formContent);
                _formScroll.StopMovement();
                _formScroll.verticalNormalizedPosition = scrollPosition;
            }
        }

        private void CreateAssetForm(string actionId, RadialFormInfo form)
        {
            if (_handler == null) return;
            var value = _handler.GetRadialFormValue(form);
            if (form.Mode == RadialActionInfo.BoolMode)
            {
                var row = new GameObject("CheckboxRow", typeof(RectTransform), typeof(LayoutElement));
                row.transform.SetParent(_formContent!, false);
                row.GetComponent<LayoutElement>().preferredHeight = 28;
                var toggle = UIFactory.CreateToggle("Checkbox", row.transform, value > 0,
                    selected => _handler?.SetRadialFormValue(actionId, form.Id, selected ? 1 : 0));
                var toggleRect = toggle.GetComponent<RectTransform>();
                toggleRect.anchorMin = toggleRect.anchorMax = new(0, 0.5f);
                toggleRect.anchoredPosition = new(12, 0);
            }
            else if (form.Mode == RadialActionInfo.IntMode && form.Options.Length > 0)
            {
                foreach (var option in form.Options)
                {
                    var selectedValue = option.Value;
                    var obj = UIFactory.CreateButton("Option", _formContent!, () =>
                    {
                        _handler?.SetRadialFormValue(actionId, form.Id, selectedValue);
                        ShowAssetForms(actionId, true);
                    }, (int)value == selectedValue ? new(0.1f, 0.5f, 0.65f) : IdleColor);
                    obj.AddComponent<LayoutElement>().preferredHeight = 32;
                    var label = UIFactory.CreateText("Label", obj.transform,
                        MinecraftFormatting.ToTmp(option.Name), alignment: TextAnchor.MiddleCenter);
                    UIFactory.SetupButtonText(label);
                    label.GetComponent<TextMeshProUGUI>().richText = true;
                }
            }
            else
            {
                var sliderObject = new GameObject("Range", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
                sliderObject.transform.SetParent(_formContent!, false);
                sliderObject.GetComponent<LayoutElement>().preferredHeight = 28;
                var track = UIFactory.CreateImage("Track", sliderObject.transform, new Color(0.25f, 0.3f, 0.35f));
                UIFactory.SetupRectTransform(track, new(0, 0.4f), new(1, 0.6f), Vector2.zero);
                var handle = UIFactory.CreateImage("Handle", sliderObject.transform, new Color(0.2f, 0.75f, 0.9f));
                var handleRect = handle.GetComponent<RectTransform>();
                handleRect.sizeDelta = new(14, 28);
                var slider = sliderObject.GetComponent<Slider>();
                slider.handleRect = handleRect;
                slider.targetGraphic = handle.GetComponent<Image>();
                slider.minValue = form.Min;
                slider.maxValue = form.Max;
                slider.wholeNumbers = form.Mode == RadialActionInfo.IntMode;
                slider.SetValueWithoutNotify((float)value);
                var number = UIFactory.CreateText("Value", _formContent!, value.ToString("0.###"), 13)
                    .GetComponent<TextMeshProUGUI>();
                number.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
                slider.onValueChanged.AddListener(selected =>
                {
                    if (_handler?.SetRadialFormValue(actionId, form.Id, selected) == true)
                        number.text = _handler.GetRadialFormValue(form).ToString("0.###");
                });
            }
        }

        private void CreateForm(YsmConfigForm form, YsmExtraAnimationButton button, int index)
        {
            var context = _runtime!.Simulation.Context;
            var value = context.ConvertToNumber(form.Evaluate(context));
            var key = FormKey(button, index);
            if (form.Kind == YsmConfigFormKind.Checkbox)
            {
                var row = new GameObject("CheckboxRow", typeof(RectTransform), typeof(LayoutElement));
                row.transform.SetParent(_formContent!, false);
                row.GetComponent<LayoutElement>().preferredHeight = 28;
                var toggle = UIFactory.CreateToggle("Checkbox", row.transform, value > 0,
                    selected => ApplyForm(() =>
                    {
                        form.SetChecked(context, selected);
                        RecordForm(key, selected ? 1 : 0);
                    }));
                var toggleRect = toggle.GetComponent<RectTransform>();
                toggleRect.anchorMin = toggleRect.anchorMax = new(0, 0.5f);
                toggleRect.anchoredPosition = new(12, 0);
            }
            else if (form.Kind == YsmConfigFormKind.Range)
            {
                if (double.IsNaN(form.Min) || double.IsNaN(form.Max) || double.IsInfinity(form.Min) ||
                    double.IsInfinity(form.Max) || form.Max < form.Min) return;
                var sliderObject =
                    new GameObject("Range", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
                sliderObject.transform.SetParent(_formContent!, false);
                sliderObject.GetComponent<LayoutElement>().preferredHeight = 28;
                var track = UIFactory.CreateImage("Track", sliderObject.transform, new Color(0.25f, 0.3f, 0.35f));
                UIFactory.SetupRectTransform(track, new(0, 0.4f), new(1, 0.6f), Vector2.zero);
                var handle = UIFactory.CreateImage("Handle", sliderObject.transform, new Color(0.2f, 0.75f, 0.9f));
                var handleRect = handle.GetComponent<RectTransform>();
                handleRect.sizeDelta = new(14, 28);
                var slider = sliderObject.GetComponent<Slider>();
                slider.handleRect = handleRect;
                slider.targetGraphic = handle.GetComponent<Image>();
                slider.minValue = (float)form.Min;
                slider.maxValue = (float)form.Max;
                slider.value = (float)value;
                var number = UIFactory.CreateText("Value", _formContent!, slider.value.ToString("0.###"), 13)
                    .GetComponent<TextMeshProUGUI>();
                number.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
                slider.onValueChanged.AddListener(selected => ApplyForm(() =>
                {
                    var snapped = form.Step > 0 && !double.IsInfinity(form.Step)
                        ? form.Min + Math.Round((selected - form.Min) / form.Step) * form.Step
                        : selected;
                    snapped = Math.Max(form.Min, Math.Min(form.Max, snapped));
                    form.SetRange(context, snapped);
                    number.text = snapped.ToString("0.###");
                    RecordForm(key, snapped);
                }));
            }
            else if (form.Kind == YsmConfigFormKind.Radio)
            {
                for (var i = 0; i < form.Labels.Count; i++)
                {
                    var optionIndex = i;
                    var obj = UIFactory.CreateButton("Option", _formContent!, () => ApplyForm(() =>
                        {
                            form.SelectRadio(context, optionIndex);
                            RecordForm(key, optionIndex);
                            ShowForms(button, true);
                        }),
                        (int)Math.Round(value) == i ? new(0.1f, 0.5f, 0.65f) : IdleColor);
                    obj.AddComponent<LayoutElement>().preferredHeight = 32;
                    var text = UIFactory.CreateText("Label", obj.transform,
                        MinecraftFormatting.ToTmp(form.Labels[i].Name), alignment: TextAnchor.MiddleCenter);
                    UIFactory.SetupButtonText(text);
                    text.GetComponent<TextMeshProUGUI>().richText = true;
                }
            }
        }

        private void ApplyForm(Action action)
        {
            if (!IsOpen || _runtime == null || !_runtime.IsAvailable) return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                ModLogger.LogWarning($"YSM configuration: {exception.Message}");
            }
        }

        private static string FormKey(YsmExtraAnimationButton button, int index)
        {
            return button.Id + ":" + index;
        }

        private void RecordForm(string key, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return;
            _formValues[key] = value;
            _saveFormsAt = Time.unscaledTime + .5f;
        }

        private void SaveForms()
        {
            if (_saveFormsAt < 0 || _settingsModelId.Length == 0) return;
            _saveFormsAt = -1;
            YsmFormSettings.Save(ModelTargetType.Character, _settingsModelId, _formValues);
        }

        private void RestoreCurrentRuntime()
        {
            var current = CurrentRuntime();
            if (ReferenceEquals(current, _settingsRuntime)) return;
            SaveForms();
            _settingsRuntime = current;
            _settingsModelId = CharacterMainControl.Main?.GetComponent<ModelHandler>()?.CurrentModelInfo?.ModelID ??
                               string.Empty;
            _formValues.Clear();
            _formDefaults.Clear();
            if (current == null || _settingsModelId.Length == 0) return;
            var context = current.Simulation.Context;
            foreach (var button in current.Presentation.Buttons)
                for (var index = 0; index < button.Forms.Count; index++)
                {
                    var form = button.Forms[index];
                    try
                    {
                        _formDefaults[FormKey(button, index)] = context.ConvertToNumber(form.Evaluate(context));
                    }
                    catch (Exception exception)
                    {
                        ModLogger.LogWarning($"YSM form default '{form.Title}': {exception.Message}");
                    }
                }

            foreach (var pair in YsmFormSettings.Load(ModelTargetType.Character, _settingsModelId))
                _formValues[pair.Key] = pair.Value;
            ApplyStoredForms(current, _formValues);
        }

        private static void ApplyStoredForms(YsmCharacterRuntime runtime, IReadOnlyDictionary<string, double> values)
        {
            var context = runtime.Simulation.Context;
            foreach (var button in runtime.Presentation.Buttons)
                for (var index = 0; index < button.Forms.Count; index++)
                {
                    var form = button.Forms[index];
                    if (!values.TryGetValue(FormKey(button, index), out var value) ||
                        double.IsNaN(value) || double.IsInfinity(value)) continue;
                    try
                    {
                        switch (form.Kind)
                        {
                            case YsmConfigFormKind.Checkbox:
                                form.SetChecked(context, value > 0);
                                break;
                            case YsmConfigFormKind.Range:
                                form.SetRange(context, Math.Max(form.Min, Math.Min(form.Max, value)));
                                break;
                            case YsmConfigFormKind.Radio:
                                var option = (int)Math.Round(value);
                                if (option >= 0 && option < form.Labels.Count) form.SelectRadio(context, option);
                                break;
                        }
                    }
                    catch (Exception exception)
                    {
                        ModLogger.LogWarning($"YSM form restore '{form.Title}': {exception.Message}");
                    }
                }
        }

        private void ResetForms()
        {
            if (_settingsRuntime == null || _settingsModelId.Length == 0) return;
            ApplyStoredForms(_settingsRuntime, _formDefaults);
            _formValues.Clear();
            _saveFormsAt = -1;
            YsmFormSettings.Save(ModelTargetType.Character, _settingsModelId, _formValues);
        }

        public void ResetCurrentFormSettings(string targetTypeId, string modelId)
        {
            if (targetTypeId == ModelTargetType.Character && modelId == _settingsModelId)
                ResetForms();
            if (_handler != null && _handler.TargetTypeId == targetTypeId && _openedModelId == modelId)
            {
                _handler.ResetRadialForms();
                if (_shownAssetActionId.Length > 0) ShowAssetForms(_shownAssetActionId, true);
            }
        }
    }
}
