using System;
using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Integrations.Ysm;
using DuckovCustomModel.MonoBehaviours;
using ModelRuntime;
using ModelRuntime.Media;
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

            if (YsmModelSource.IsYsm(modelInfo))
                return GetYsmHeight(bundleInfo, modelInfo);

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

            if (YsmModelSource.IsYsm(modelInfo))
            {
                using var lease = YsmModelSource.Acquire(bundleInfo, modelInfo);
                var scale = lease.Package.Models[lease.Profile.ModelTarget].DisplayScale;
                return new Vector3(scale.X, scale.Y, scale.Z) * lease.Profile.Scale;
            }

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

            if (handler.YsmRuntime == null) GetHelmetHeightFromPrefab(modelID);
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

            if (handler.YsmRuntime != null)
            {
                var runtime = handler.YsmRuntime;
                var requested = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelID)
                    .GetValue<float>("UserHeight");
                handler.ApplyHeightFromRuntimeData(requested > 0 ? requested : runtime.NaturalHeight,
                    runtime.NaturalHeight, runtime.InitialRootScale);
                return;
            }

            var userHeight = GetHeight(targetTypeId, modelID);
            var initialHeight = GetHelmetHeightFromPrefab(modelID);
            var initialRootScale = GetInitialRootScaleFromPrefab(modelID);

            if (userHeight <= 0 || initialHeight <= 0)
                return;

            handler.ApplyHeightFromRuntimeData(userHeight, initialHeight, initialRootScale);
        }

        private static float GetYsmHeight(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            foreach (var handler in ModelManager.GetAllHandlers())
                if (handler.CurrentModelInfo?.ModelID == modelInfo.ModelID && handler.YsmRuntime != null)
                    return handler.YsmRuntime.NaturalHeight;
            var pixels = new StandardTexturePixelSource();
            try
            {
                using var lease = YsmModelSource.Acquire(bundleInfo, modelInfo);
                var document = lease.Package.Models[lease.Profile.ModelTarget];
                var pose = new PoseEvaluator(document);
                pose.Evaluate(ReadOnlySpan<AnimationLayer>.Empty, new());
                var heightScale = Math.Abs(document.DisplayScale.Y * lease.Profile.Scale) / 16f;
                var locator = lease.Profile.LocatorMappings.TryGetValue(SocketNames.Helmet, out var mapped)
                    ? mapped
                    : SocketNames.Helmet;
                if (!string.IsNullOrEmpty(locator) && pose.TryGetLocatorTransform(locator, out var transform) &&
                    transform.M42 > 0)
                    return transform.M42 * heightScale;
                var mesh = MeshBaker.Bake(document, pixels);
                var minimum = 0f;
                var maximum = 0f;
                for (var i = 0; i < mesh.Positions.Length; i++)
                {
                    var position =
                        System.Numerics.Vector3.Transform(mesh.Positions[i], pose.Matrices[mesh.BoneIndices[i]]);
                    minimum = Math.Min(minimum, position.Y);
                    maximum = Math.Max(maximum, position.Y);
                }

                return (maximum - minimum) * heightScale;
            }
            catch (Exception exception)
            {
                ModLogger.LogWarning($"Unable to measure YSM model height: {exception.Message}");
                return 0;
            }
            finally
            {
                pixels.Clear();
            }
        }
    }
}
