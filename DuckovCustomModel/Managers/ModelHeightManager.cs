using System;
using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;

namespace DuckovCustomModel.Managers
{
    public static class ModelHeightManager
    {
        public static event Action<string, string>? OnHeightChanged;

        private static Transform? SearchLocatorTransform(Transform root, string locatorName)
        {
            if (root == null || string.IsNullOrWhiteSpace(locatorName))
                return null;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            return transforms.FirstOrDefault(t => t.name == locatorName);
        }

        private static float GetHelmetHeightFromPrefab(string modelID)
        {
            if (string.IsNullOrWhiteSpace(modelID))
                return 0f;

            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo)) return 0f;

            var prefab = AssetBundleManager.LoadAssetFromBundle<GameObject>(bundleInfo, modelInfo.PrefabPath);
            if (prefab == null) return 0f;

            var helmetLocator = SearchLocatorTransform(prefab.transform, SocketNames.Helmet);
            if (helmetLocator == null) return 0f;

            var height = helmetLocator.position.y - prefab.transform.position.y;
            return height;
        }

        private static Vector3 GetInitialRootScaleFromPrefab(string modelID)
        {
            if (string.IsNullOrWhiteSpace(modelID) ||
                !ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo))
                return Vector3.one;

            var prefab = AssetBundleManager.LoadAssetFromBundle<GameObject>(bundleInfo, modelInfo.PrefabPath);
            return prefab == null ? Vector3.one : prefab.transform.localScale;
        }

        public static bool HasHelmetLocator(string modelID)
        {
            if (string.IsNullOrWhiteSpace(modelID))
                return false;

            return GetHelmetHeightFromPrefab(modelID) > 0;
        }

        public static float GetHeight(string targetTypeId, string modelID)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
                return 0f;

            var runtimeData = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelID);
            var userHeight = runtimeData.GetValue<float>("UserHeight");

            return userHeight > 0 ? userHeight : GetHelmetHeightFromPrefab(modelID);
        }

        public static void SetHeight(string targetTypeId, string modelID, float height)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
                return;

            if (height <= 0)
            {
                ModLogger.LogWarning($"Invalid height: {height}. Must be greater than 0.");
                return;
            }

            var runtimeData = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelID);
            runtimeData.SetValue("UserHeight", height);
            ModelRuntimeDataManager.SaveRuntimeData(targetTypeId, modelID, runtimeData);

            OnHeightChanged?.Invoke(targetTypeId, modelID);
        }

        public static void ResetHeight(string targetTypeId, string modelID)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
                return;

            var initialHeight = GetHelmetHeightFromPrefab(modelID);
            if (initialHeight > 0) SetHeight(targetTypeId, modelID, initialHeight);
        }

        public static void InitializeHeightForHandler(ModelHandler handler)
        {
            if (handler == null || handler.CustomModelInstance == null)
                return;

            var targetTypeId = handler.TargetTypeId;
            var modelID = handler.CurrentModelInfo?.ModelID;

            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
                return;

            GetHelmetHeightFromPrefab(modelID);
            ApplyHeightToHandler(handler);
        }

        public static void ApplyHeightToHandler(ModelHandler handler)
        {
            if (handler == null || handler.CustomModelInstance == null)
                return;

            var targetTypeId = handler.TargetTypeId;
            var modelID = handler.CurrentModelInfo?.ModelID;

            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
                return;

            var userHeight = GetHeight(targetTypeId, modelID);
            var initialHeight = GetHelmetHeightFromPrefab(modelID);
            var initialRootScale = GetInitialRootScaleFromPrefab(modelID);

            if (userHeight <= 0 || initialHeight <= 0)
                return;

            handler.ApplyHeightFromRuntimeData(userHeight, initialHeight, initialRootScale);
        }
    }
}
