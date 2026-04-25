using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DuckovCustomModel.Managers
{
    public class InputBlocker : MonoBehaviour
    {
        internal static bool IsGettingRealInput;
        private float _lastActionsNullLogTime;
        private float _lastActivateFailLogTime;
        private float _lastDeactivateFailLogTime;
        private PlayerInput? _playerInput;
        internal bool IsBlocked;
        internal bool IsBlockerCalling;
        internal bool IsExternalBlocking;

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
            if (Instance == this) Instance = null;
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
                return Input.GetKeyDown(key);
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
                return Input.GetKey(key);
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
                return Input.GetKeyUp(key);
            }
            finally
            {
                IsGettingRealInput = false;
            }
        }
    }
}
