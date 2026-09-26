using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DuckovCustomModel.Managers
{
    public class InputBlocker : MonoBehaviour
    {
        internal static bool IsGettingRealInput;
        private readonly Dictionary<InputAction, bool> _modalPreviousActions = new();
        private readonly HashSet<InputAction> _modalUiActions = new();
        internal bool IsBlocked;
        internal bool IsBlockerCalling;
        internal bool IsExternalBlocking;
        private float _lastActionsNullLogTime;
        private float _lastActivateFailLogTime;
        private float _lastDeactivateFailLogTime;
        private bool _modalUiOpen;
        private bool _modalWasBlocked;
        private PlayerInput? _playerInput;

        public static bool IsInputBlocked => Instance != null && Instance.IsBlocked;

        public static InputBlocker? Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            UpdatePlayerInputState();
        }

        private void OnDestroy()
        {
            EndModalUi();
            if (Instance == this) Instance = null;
        }

        public void BeginModalUi(InputSystemUIInputModule? module)
        {
            if (_modalUiOpen) return;
            _modalWasBlocked = IsBlocked;
            _modalUiOpen = true;
            _modalPreviousActions.Clear();
            _modalUiActions.Clear();
            if (module != null)
            {
                KeepUiAction(module.point);
                KeepUiAction(module.leftClick);
                KeepUiAction(module.rightClick);
                KeepUiAction(module.middleClick);
                KeepUiAction(module.scrollWheel);
                KeepUiAction(module.move);
                KeepUiAction(module.submit);
                KeepUiAction(module.cancel);
                KeepUiAction(module.trackedDevicePosition);
                KeepUiAction(module.trackedDeviceOrientation);
            }

            var actions = GameManager.MainPlayerInput?.actions;
            if (actions != null)
                foreach (var action in actions)
                    _modalPreviousActions[action] = action.enabled;
            IsBlocked = true;
            UpdatePlayerInputState();
        }

        public void EndModalUi()
        {
            if (!_modalUiOpen) return;
            _modalUiOpen = false;
            IsBlocked = _modalWasBlocked;
            foreach (var entry in _modalPreviousActions)
            {
                if (entry.Key == null) continue;
                if (entry.Value && !IsBlocked && !IsExternalBlocking) entry.Key.Enable();
                else if (!entry.Value) entry.Key.Disable();
            }

            _modalPreviousActions.Clear();
            _modalUiActions.Clear();
        }

        private void KeepUiAction(InputActionReference? reference)
        {
            if (reference?.action != null) _modalUiActions.Add(reference.action);
        }

        public static void BlockInput()
        {
            if (Instance == null) return;

            Instance.IsBlocked = true;
        }

        public static void UnblockInput()
        {
            if (Instance == null) return;

            Instance.IsBlocked = false;
        }

        private void UpdatePlayerInputState()
        {
            var playerInput = GameManager.MainPlayerInput;
            if (playerInput == null)
            {
                _playerInput = null;
                return;
            }

            if (_playerInput == null || _playerInput != playerInput) _playerInput = playerInput;

            var shouldBeActive = !IsBlocked && !IsExternalBlocking;

            var actions = playerInput.actions;
            if (actions == null)
            {
                if (Time.unscaledTime - _lastActionsNullLogTime > 2f)
                {
                    _lastActionsNullLogTime = Time.unscaledTime;
                    ModLogger.LogWarning("InputBlocker: PlayerInput.actions is null, skip input toggle this frame.");
                }

                return;
            }

            if (_modalUiOpen)
            {
                foreach (var action in actions)
                    if (_modalUiActions.Contains(action))
                    {
                        if (!action.enabled) action.Enable();
                    }
                    else if (action.enabled)
                    {
                        action.Disable();
                    }

                return;
            }

            var actionsEnabled = actions.enabled;

            if (shouldBeActive && !actionsEnabled)
            {
                IsBlockerCalling = true;
                try
                {
                    try
                    {
                        actions.Enable();
                    }
                    catch (Exception e)
                    {
                        if (Time.unscaledTime - _lastActivateFailLogTime > 2f)
                        {
                            _lastActivateFailLogTime = Time.unscaledTime;
                            ModLogger.LogWarning($"InputBlocker actions.Enable() failed: {e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Time.unscaledTime - _lastActivateFailLogTime > 2f)
                    {
                        _lastActivateFailLogTime = Time.unscaledTime;
                        ModLogger.LogWarning($"InputBlocker unexpected enable exception: {e}");
                    }
                }
                finally
                {
                    IsBlockerCalling = false;
                }
            }
            else if (!shouldBeActive && actionsEnabled)
            {
                IsBlockerCalling = true;
                try
                {
                    try
                    {
                        actions.Disable();
                    }
                    catch (Exception e)
                    {
                        if (Time.unscaledTime - _lastDeactivateFailLogTime > 2f)
                        {
                            _lastDeactivateFailLogTime = Time.unscaledTime;
                            ModLogger.LogWarning($"InputBlocker actions.Disable() failed: {e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Time.unscaledTime - _lastDeactivateFailLogTime > 2f)
                    {
                        _lastDeactivateFailLogTime = Time.unscaledTime;
                        ModLogger.LogWarning($"InputBlocker unexpected disable exception: {e}");
                    }
                }
                finally
                {
                    IsBlockerCalling = false;
                }
            }
        }


        public static bool GetRealKeyDown(KeyCode key)
        {
            IsGettingRealInput = true;
            try
            {
                return InputCompatibility.GetKeyDown(key);
            }
            finally
            {
                IsGettingRealInput = false;
            }
        }

        public static bool GetRealKey(KeyCode key)
        {
            IsGettingRealInput = true;
            try
            {
                return InputCompatibility.GetKey(key);
            }
            finally
            {
                IsGettingRealInput = false;
            }
        }

        public static bool GetRealKeyUp(KeyCode key)
        {
            IsGettingRealInput = true;
            try
            {
                return InputCompatibility.GetKeyUp(key);
            }
            finally
            {
                IsGettingRealInput = false;
            }
        }
    }
}
