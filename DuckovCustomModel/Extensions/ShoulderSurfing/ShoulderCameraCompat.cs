using System.Reflection;
using Duckov.Modding;
using UnityEngine;

namespace DuckovCustomModel.Extensions.ShoulderSurfing
{
    public class ShoulderCameraCompat
    {
        private static Transform _shoulderCameraTransform = null!;
        private static Component _shoulderCamera = null!;
        private static FieldInfo? _cameraPitchFieldInfoField;
        private static FieldInfo? _shoulderCameraToggledFieldInfo;
        private static float _cachedCameraPitch;
        private static int _lastUpdateFrame = -1;

        public static bool CheckShoulderCameraInstalled()
        {
            if (_shoulderCameraTransform != null) return true;

            if (ModManager.Instance == null) return false;
            var shoulderSurfing = ModManager.Instance.transform.Find("ShoulderSurfing");
            if (shoulderSurfing == null) return false;
            _shoulderCameraTransform = shoulderSurfing.transform;
            return true;
        }

        public static bool CheckShoulderCameraActive()
        {
            if (!CheckShoulderCameraInstalled()) return false;

            if (_shoulderCamera != null) return true;

            // ReSharper disable once Unity.UnresolvedComponentOrScriptableObject
            _shoulderCamera = _shoulderCameraTransform.GetComponent("ShoulderSurfing.ShoulderCamera");
            return _shoulderCamera != null;
        }

        public static float GetCameraPitch()
        {
            if (!CheckShoulderCameraActive()) return 0f;

            if (_shoulderCameraToggledFieldInfo == null)
                _shoulderCameraToggledFieldInfo = _shoulderCamera.GetType()
                    .GetField("shoulderCameraToggled",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

            var toggled = (bool)_shoulderCameraToggledFieldInfo.GetValue(null)!;
            if (!toggled) return 0f;

            var currentFrame = Time.frameCount;
            if (_lastUpdateFrame == currentFrame)
                return _cachedCameraPitch;

            if (_cameraPitchFieldInfoField == null)
                _cameraPitchFieldInfoField = _shoulderCamera.GetType()
                    .GetField("cameraPitch", BindingFlags.NonPublic | BindingFlags.Instance)!;

            _cachedCameraPitch = (float)_cameraPitchFieldInfoField.GetValue(_shoulderCamera)!;
            _lastUpdateFrame = currentFrame;
            return _cachedCameraPitch;
        }
    }
}
