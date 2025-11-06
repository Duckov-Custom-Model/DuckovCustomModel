using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomModel.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Managers
{
    public static class AssetBundleManager
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = [];

        public static AssetBundle? GetOrLoadAssetBundle(ModelBundleInfo bundleInfo, bool forceReload = false)
        {
            var bundlePath = bundleInfo.BundlePath;
            if (string.IsNullOrEmpty(bundlePath))
            {
                Debug.LogError("AssetBundleManager: bundlePath is null or empty.");
                return null;
            }

            if (!forceReload && LoadedBundles.TryGetValue(bundlePath, out var existingBundle)) return existingBundle;

            try
            {
                var bundleData = File.ReadAllBytes(bundlePath);
                var assetBundle = AssetBundle.LoadFromMemory(bundleData);
                if (assetBundle == null)
                {
                    ModLogger.LogError($"AssetBundleManager: Failed to load AssetBundle from path: {bundlePath}");
                    return null;
                }

                if (LoadedBundles.TryGetValue(bundlePath, out var oldBundle))
                {
                    oldBundle.Unload(true);
                    LoadedBundles.Remove(bundlePath);
                }

                LoadedBundles[bundlePath] = assetBundle;
                return assetBundle;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading AssetBundle from path: {bundlePath}. Exception: {ex}");
                return null;
            }
        }

        public static T? LoadAssetFromBundle<T>(ModelBundleInfo bundleInfo, string assetPath) where T : Object
        {
            var bundle = GetOrLoadAssetBundle(bundleInfo);
            if (bundle == null) return null;

            try
            {
                var asset = bundle.LoadAsset<T>(assetPath);
                if (asset == null)
                    ModLogger.LogError(
                        $"AssetBundleManager: Failed to load asset '{assetPath}' from bundle '{bundleInfo.BundlePath}'");
                return asset;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading asset '{assetPath}' from bundle '{bundleInfo.BundlePath}'. Exception: {ex}");
                return null;
            }
        }

        public static GameObject? LoadModelPrefab(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            return LoadAssetFromBundle<GameObject>(bundleInfo, modelInfo.PrefabPath);
        }

        public static Texture2D? LoadThumbnailTexture(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            return string.IsNullOrEmpty(modelInfo.ThumbnailPath)
                ? null
                : LoadAssetFromBundle<Texture2D>(bundleInfo, modelInfo.ThumbnailPath);
        }

        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var bundle in LoadedBundles.Values) bundle.Unload(unloadAllLoadedObjects);
            LoadedBundles.Clear();
        }
    }
}