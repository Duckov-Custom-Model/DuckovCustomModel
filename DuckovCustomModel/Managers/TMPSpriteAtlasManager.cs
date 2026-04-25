using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using TMPro;

namespace DuckovCustomModel.Managers
{
    public static class TMPSpriteAtlasManager
    {
        private static readonly Dictionary<string, TMP_SpriteAsset> LoadedSpriteAssets = [];
        private static readonly HashSet<TMP_SpriteAsset> RegisteredAssets = [];

        public static void LoadAndRegisterSpriteAtlases(ModelBundleInfo bundleInfo)
        {
            if (bundleInfo.SpriteAtlasPaths == null || bundleInfo.SpriteAtlasPaths.Length == 0)
                return;

            var spriteAssets = AssetBundleManager.LoadSpriteAtlases<TMP_SpriteAsset>(bundleInfo);
            if (spriteAssets == null || spriteAssets.Length == 0)
                return;

            foreach (var spriteAsset in spriteAssets)
            {
                if (spriteAsset == null) continue;

                try
                {
                    RegisterSpriteAsset(bundleInfo.BundleName, spriteAsset);
                }
                catch (Exception ex)
                {
                    ModLogger.LogError(
                        $"TMPSpriteAtlasManager: Exception while registering sprite asset '{spriteAsset.name}' from bundle '{bundleInfo.BundleName}'. Exception: {ex}");
                }
            }
        }

        public static async UniTask LoadAndRegisterSpriteAtlasesAsync(ModelBundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            if (bundleInfo.SpriteAtlasPaths == null || bundleInfo.SpriteAtlasPaths.Length == 0)
                return;

            var spriteAssets =
                await AssetBundleManager.LoadSpriteAtlasesAsync<TMP_SpriteAsset>(bundleInfo, cancellationToken);
            if (spriteAssets == null || spriteAssets.Length == 0)
                return;

            foreach (var spriteAsset in spriteAssets)
            {
                if (spriteAsset == null) continue;

                try
                {
                    RegisterSpriteAsset(bundleInfo.BundleName, spriteAsset);
                }
                catch (Exception ex)
                {
                    ModLogger.LogError(
                        $"TMPSpriteAtlasManager: Exception while registering sprite asset '{spriteAsset.name}' from bundle '{bundleInfo.BundleName}'. Exception: {ex}");
                }
            }
        }

        private static void RegisterSpriteAsset(string bundleName, TMP_SpriteAsset spriteAsset)
        {
            var key = $"{bundleName}:{spriteAsset.name}";

            if (LoadedSpriteAssets.ContainsKey(key))
            {
                ModLogger.LogWarning(
                    $"TMPSpriteAtlasManager: Sprite asset '{spriteAsset.name}' from bundle '{bundleName}' is already registered. Skipping.");
                return;
            }

            LoadedSpriteAssets[key] = spriteAsset;

            if (!RegisteredAssets.Contains(spriteAsset))
            {
                TMP_Settings.defaultSpriteAsset?.fallbackSpriteAssets?.Add(spriteAsset);
                RegisteredAssets.Add(spriteAsset);
                ModLogger.Log(
                    $"TMPSpriteAtlasManager: Registered sprite asset '{spriteAsset.name}' from bundle '{bundleName}'");
            }
        }

        public static void UnregisterSpriteAssets(string bundleName)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in LoadedSpriteAssets)
                if (kvp.Key.StartsWith($"{bundleName}:"))
                    keysToRemove.Add(kvp.Key);

            foreach (var key in keysToRemove)
            {
                if (!LoadedSpriteAssets.TryGetValue(key, out var spriteAsset)) continue;

                LoadedSpriteAssets.Remove(key);

                if (RegisteredAssets.Contains(spriteAsset))
                {
                    TMP_Settings.defaultSpriteAsset?.fallbackSpriteAssets?.Remove(spriteAsset);
                    RegisteredAssets.Remove(spriteAsset);
                    ModLogger.Log(
                        $"TMPSpriteAtlasManager: Unregistered sprite asset '{spriteAsset.name}' from bundle '{bundleName}'");
                }
            }
        }

        public static void UnregisterAllSpriteAssets()
        {
            foreach (var spriteAsset in RegisteredAssets)
                TMP_Settings.defaultSpriteAsset?.fallbackSpriteAssets?.Remove(spriteAsset);

            RegisteredAssets.Clear();
            LoadedSpriteAssets.Clear();
            ModLogger.Log("TMPSpriteAtlasManager: Unregistered all sprite assets");
        }

        public static TMP_SpriteAsset? GetSpriteAsset(string bundleName, string assetName)
        {
            var key = $"{bundleName}:{assetName}";
            return LoadedSpriteAssets.TryGetValue(key, out var spriteAsset) ? spriteAsset : null;
        }

        public static IReadOnlyDictionary<string, TMP_SpriteAsset> GetAllLoadedSpriteAssets()
        {
            return LoadedSpriteAssets;
        }
    }
}
