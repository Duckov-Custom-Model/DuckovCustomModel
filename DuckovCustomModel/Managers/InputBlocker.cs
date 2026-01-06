using UnityEngine;
using UnityEngine.InputSystem;

namespace DuckovCustomModel.Managers
{
    public class InputBlocker : MonoBehaviour
    {
        internal static bool IsGettingRealInput;
        private PlayerInput? _playerInput;
        private bool _playerInputWasActive;
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
                _playerInputWasActive = false;
                return;
            }

            if (_playerInput == null || _playerInput != playerInput)
            {
                _playerInput = playerInput;
                _playerInputWasActive = playerInput.inputIsActive;
            }

            var shouldBeActive = !IsBlocked && !IsExternalBlocking;

            if (shouldBeActive && !playerInput.inputIsActive)
            {
                IsBlockerCalling = true;
                try
                {
                    playerInput.ActivateInput();
                }
                finally
                {
                    IsBlockerCalling = false;
                }
            }
            else if (!shouldBeActive && playerInput.inputIsActive)
            {
                IsBlockerCalling = true;
                try
                {
                    playerInput.DeactivateInput();
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
