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

        public static float CameraPitch { get; private set; }

        public static bool IsActive { get; private set; }

        public static void Initialize()
        {
            _shoulderCameraTransform = null;
            _shoulderCamera = null;
            _cameraPitchFieldInfo = null;
            _shoulderCameraToggledFieldInfo = null;
            CameraPitch = 0f;
            IsActive = false;

            RegisterModActivatedEvents();
            TryFindShoulderCamera();
        }

        private static void TryFindShoulderCamera()
        {
            if (IsActive) return;

            if (ModManager.Instance == null) return;

            var shoulderSurfing = ModManager.Instance.transform.Find("ShoulderSurfing");
            if (shoulderSurfing == null) return;

            _shoulderCameraTransform = shoulderSurfing.transform;

            // ReSharper disable once Unity.UnresolvedComponentOrScriptableObject
            _shoulderCamera = _shoulderCameraTransform.GetComponent("ShoulderSurfing.ShoulderCamera");
            if (_shoulderCamera == null) return;

            var cameraType = _shoulderCamera.GetType();
            _shoulderCameraToggledFieldInfo = cameraType.GetField("shoulderCameraToggled",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _cameraPitchFieldInfo = cameraType.GetField("cameraPitch",
                BindingFlags.NonPublic | BindingFlags.Instance);

            IsActive = _shoulderCameraToggledFieldInfo != null && _cameraPitchFieldInfo != null;
        }

        public static void UpdateState()
        {
            if (!IsActive)
            {
                CameraPitch = 0f;
                return;
            }

            if (_shoulderCameraTransform == null || _shoulderCamera == null ||
                _shoulderCameraToggledFieldInfo == null || _cameraPitchFieldInfo == null)
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
            UnregisterModActivatedEvents();
            IsActive = false;
            _shoulderCameraTransform = null;
            _shoulderCamera = null;
            _cameraPitchFieldInfo = null;
            _shoulderCameraToggledFieldInfo = null;
            CameraPitch = 0f;
        }

        private static void RegisterModActivatedEvents()
        {
            UnregisterModActivatedEvents();
            ModManager.OnModActivated += OnModActivated;
        }

        private static void UnregisterModActivatedEvents()
        {
            ModManager.OnModActivated -= OnModActivated;
        }

        private static void OnModActivated(ModInfo modInfo, ModBehaviour modBehaviour)
        {
            if (IsActive) return;

            TryFindShoulderCamera();
        }
    }
}
