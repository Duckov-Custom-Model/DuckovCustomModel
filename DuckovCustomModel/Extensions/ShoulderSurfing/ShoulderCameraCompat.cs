using System.Reflection;
using Duckov.Modding;
using UnityEngine;

namespace DuckovCustomModel.Extensions.ShoulderSurfing
{
    public static class ShoulderCameraCompat
    {
        private static Transform? _shoulderCameraTransform;
        private static Component? _shoulderCamera;
        private static FieldInfo? _cameraPitchFieldInfo;
        private static FieldInfo? _shoulderCameraToggledFieldInfo;
        private static bool _isInitialized;

        public static float CameraPitch { get; private set; }

        public static bool IsActive { get; private set; }

        public static bool Initialize()
        {
            if (_isInitialized && IsActive) return true;

            _isInitialized = true;
            _shoulderCameraTransform = null;
            _shoulderCamera = null;
            _cameraPitchFieldInfo = null;
            _shoulderCameraToggledFieldInfo = null;
            CameraPitch = 0f;
            IsActive = false;

            if (ModManager.Instance == null) return false;

            var shoulderSurfing = ModManager.Instance.transform.Find("ShoulderSurfing");
            if (shoulderSurfing == null) return false;

            _shoulderCameraTransform = shoulderSurfing.transform;

            // ReSharper disable once Unity.UnresolvedComponentOrScriptableObject
            _shoulderCamera = _shoulderCameraTransform.GetComponent("ShoulderSurfing.ShoulderCamera");
            if (_shoulderCamera == null) return false;

            var cameraType = _shoulderCamera.GetType();
            _shoulderCameraToggledFieldInfo = cameraType.GetField("shoulderCameraToggled",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _cameraPitchFieldInfo = cameraType.GetField("cameraPitch",
                BindingFlags.NonPublic | BindingFlags.Instance);

            IsActive = _shoulderCameraToggledFieldInfo != null && _cameraPitchFieldInfo != null;
            return IsActive;
        }

        public static void UpdateState()
        {
            if (!_isInitialized || !IsActive)
                if (!Initialize())
                {
                    CameraPitch = 0f;
                    return;
                }

            if (_shoulderCamera == null || _shoulderCameraToggledFieldInfo == null || _cameraPitchFieldInfo == null)
            {
                IsActive = false;
                CameraPitch = 0f;
                return;
            }

            var toggled = (bool)_shoulderCameraToggledFieldInfo.GetValue(null)!;
            if (!toggled)
            {
                CameraPitch = 0f;
                return;
            }

            CameraPitch = (float)_cameraPitchFieldInfo.GetValue(_shoulderCamera)!;
        }

        public static void Cleanup()
        {
            _isInitialized = false;
            _shoulderCameraTransform = null;
            _shoulderCamera = null;
            _cameraPitchFieldInfo = null;
            _shoulderCameraToggledFieldInfo = null;
            CameraPitch = 0f;
        }
    }
}
