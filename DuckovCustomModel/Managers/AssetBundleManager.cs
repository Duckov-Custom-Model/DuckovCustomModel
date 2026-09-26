using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Integrations.Ysm;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Managers
{
    public static class AssetBundleManager
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = [];
        private static readonly Dictionary<string, UniTask<AssetBundle?>> LoadingTasks = [];
        private static readonly Dictionary<AssetBundle, HashSet<string>> AssetNames = [];

        public static AssetBundle? GetOrLoadAssetBundle(ModelBundleInfo bundleInfo, bool forceReload = false)
        {
            if (YsmModelSource.IsYsm(bundleInfo)) return null;
            var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
            if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
            {
                ModLogger.LogError($"AssetBundleManager: AssetBundle file not found at path: {bundlePath}");
                return null;
            }

            if (!forceReload && LoadedBundles.TryGetValue(bundlePath, out var existingBundle)) return existingBundle;

            try
            {
                if (forceReload && LoadedBundles.TryGetValue(bundlePath, out var oldBundleToUnload))
                {
                    AssetNames.Remove(oldBundleToUnload);
                    oldBundleToUnload.Unload(true);
                    LoadedBundles.Remove(bundlePath);
                }

                var assetBundle = AssetBundle.LoadFromFile(bundlePath);
                if (assetBundle == null)
                {
                    ModLogger.LogError($"AssetBundleManager: Failed to load AssetBundle from path: {bundlePath}");
                    return null;
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

        public static async UniTask<AssetBundle?> GetOrLoadAssetBundleAsync(ModelBundleInfo bundleInfo,
            bool forceReload = false, CancellationToken cancellationToken = default)
        {
            if (YsmModelSource.IsYsm(bundleInfo)) return null;
            var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
            if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
            {
                ModLogger.LogError($"AssetBundleManager: AssetBundle file not found at path: {bundlePath}");
                return null;
            }

            if (!forceReload && LoadedBundles.TryGetValue(bundlePath, out var existingBundle))
                return existingBundle;

            if (LoadingTasks.TryGetValue(bundlePath, out var loadingTask))
                return await loadingTask;

            var task = LoadAssetBundleInternalAsync(bundlePath, forceReload, cancellationToken);
            LoadingTasks[bundlePath] = task;

            try
            {
                var result = await task;
                return result;
            }
            finally
            {
                LoadingTasks.Remove(bundlePath);
            }
        }

        private static async UniTask<AssetBundle?> LoadAssetBundleInternalAsync(string bundlePath, bool forceReload,
            CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            try
            {
                if (forceReload && LoadedBundles.TryGetValue(bundlePath, out var oldBundleToUnload))
                {
                    AssetNames.Remove(oldBundleToUnload);
                    oldBundleToUnload.Unload(true);
                    LoadedBundles.Remove(bundlePath);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                var request = AssetBundle.LoadFromFileAsync(bundlePath);
                await request.ToUniTask();
                var assetBundle = request.assetBundle;
                if (cancellationToken.IsCancellationRequested)
                {
                    if (assetBundle != null) assetBundle.Unload(true);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (assetBundle == null)
                {
                    ModLogger.LogError($"AssetBundleManager: Failed to load AssetBundle from path: {bundlePath}");
                    return null;
                }

                if (LoadedBundles.TryGetValue(bundlePath, out var oldBundle))
                {
                    AssetNames.Remove(oldBundle);
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

        public static void UnloadAssetBundle(string bundlePath)
        {
            if (string.IsNullOrEmpty(bundlePath)) return;

            if (LoadedBundles.TryGetValue(bundlePath, out var bundle))
            {
                AssetNames.Remove(bundle);
                bundle.Unload(true);
                LoadedBundles.Remove(bundlePath);
            }

            LoadingTasks.Remove(bundlePath, out _);
        }

        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var bundle in LoadedBundles.Values) bundle.Unload(unloadAllLoadedObjects);
            LoadedBundles.Clear();
            LoadingTasks.Clear();
            AssetNames.Clear();
        }

        public static T? LoadAssetFromBundle<T>(ModelBundleInfo bundleInfo, string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

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

        public static async UniTask<GameObject?> LoadModelPrefabAsync(ModelBundleInfo bundleInfo, ModelInfo modelInfo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(modelInfo.PrefabPath)) return null;

            var bundle = await GetOrLoadAssetBundleAsync(bundleInfo, false, cancellationToken);
            if (bundle == null) return null;

            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                var asset = bundle.LoadAsset<GameObject>(modelInfo.PrefabPath);
                if (asset == null)
                    ModLogger.LogError(
                        $"AssetBundleManager: Failed to load asset '{modelInfo.PrefabPath}' from bundle '{bundleInfo.BundlePath}'");
                return asset;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading asset '{modelInfo.PrefabPath}' from bundle '{bundleInfo.BundlePath}'. Exception: {ex}");
                return null;
            }
        }

        public static GameObject? LoadDeathLootBoxPrefab(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            return LoadAssetFromBundle<GameObject>(bundleInfo, modelInfo.DeathLootBoxPrefabPath ?? string.Empty);
        }

        public static Texture2D? LoadThumbnailTexture(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            if (modelInfo.ThumbnailData is { Length: > 0 }) return LoadThumbnailBytes(modelInfo.ThumbnailData);
            if (string.IsNullOrEmpty(modelInfo.ThumbnailPath)) return null;

            try
            {
                if (Path.IsPathRooted(modelInfo.ThumbnailPath)) return LoadTextureFromFile(modelInfo.ThumbnailPath);

                var externalPath = Path.Combine(bundleInfo.DirectoryPath, modelInfo.ThumbnailPath);
                return File.Exists(externalPath) ? LoadTextureFromFile(externalPath) : null;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading thumbnail '{modelInfo.ThumbnailPath}'. Exception: {ex}");
                return null;
            }
        }

        public static async UniTask<Texture2D?> LoadThumbnailTextureAsync(ModelBundleInfo bundleInfo,
            ModelInfo modelInfo, CancellationToken cancellationToken = default)
        {
            if (modelInfo.ThumbnailData is { Length: > 0 })
            {
                var embedded = LoadThumbnailBytes(modelInfo.ThumbnailData);
                if (embedded != null || !YsmModelSource.IsYsm(modelInfo)) return embedded;
            }

            if (string.IsNullOrEmpty(modelInfo.ThumbnailPath))
                return null;

            try
            {
                if (Path.IsPathRooted(modelInfo.ThumbnailPath))
                    return await LoadTextureFromFileAsync(modelInfo.ThumbnailPath, cancellationToken);

                var externalPath = Path.Combine(bundleInfo.DirectoryPath, modelInfo.ThumbnailPath);
                return File.Exists(externalPath)
                    ? await LoadTextureFromFileAsync(externalPath, cancellationToken)
                    : null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading thumbnail '{modelInfo.ThumbnailPath}'. Exception: {ex}");
                return null;
            }
        }

        public static bool CheckPrefabExists(ModelBundleInfo bundleInfo, ModelInfo modelInfo)
        {
            if (string.IsNullOrEmpty(modelInfo.PrefabPath)) return false;

            var bundle = GetOrLoadAssetBundle(bundleInfo);
            return bundle != null && CheckAssetExistsInBundle(bundle, modelInfo.PrefabPath);
        }

        public static (bool isValid, string? errorMessage) CheckBundleStatus(ModelBundleInfo bundleInfo,
            ModelInfo modelInfo)
        {
            var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
            if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
                return (false, $"AssetBundle file not found: {bundleInfo.BundlePath}");

            if (string.IsNullOrEmpty(modelInfo.PrefabPath)) return (false, "Prefab path is not configured");

            try
            {
                var bundle = GetOrLoadAssetBundle(bundleInfo);
                if (bundle == null) return (false, "Failed to load AssetBundle");

                return !CheckAssetExistsInBundle(bundle, modelInfo.PrefabPath)
                    ? (false, $"Prefab not found in bundle: {modelInfo.PrefabPath}")
                    : (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async UniTask<(bool isValid, string? errorMessage)> CheckBundleStatusAsync(
            ModelBundleInfo bundleInfo,
            ModelInfo modelInfo, CancellationToken cancellationToken = default)
        {
            if (YsmModelSource.IsYsm(modelInfo))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // Discovery already validates the package. UI status checks never decode it again.
                    YsmModelSource.GetSourcePath(bundleInfo, modelInfo);
                    return string.IsNullOrEmpty(modelInfo.SourceRevision)
                        ? (false, "Refresh the model list to validate this YSM source")
                        : (true, null);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
            if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
                return (false, $"AssetBundle file not found: {bundleInfo.BundlePath}");

            if (string.IsNullOrEmpty(modelInfo.PrefabPath)) return (false, "Prefab path is not configured");

            try
            {
                var bundle = await GetOrLoadAssetBundleAsync(bundleInfo, false, cancellationToken);
                if (bundle == null) return (false, "Failed to load AssetBundle");

                return !CheckAssetExistsInBundle(bundle, modelInfo.PrefabPath)
                    ? (false, $"Prefab not found in bundle: {modelInfo.PrefabPath}")
                    : (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static bool CheckAssetExistsInBundle(AssetBundle bundle, string assetPath)
        {
            if (!AssetNames.TryGetValue(bundle, out var assetNames))
            {
                assetNames = new(bundle.GetAllAssetNames(), StringComparer.OrdinalIgnoreCase);
                AssetNames.Add(bundle, assetNames);
            }

            return assetNames.Contains(assetPath);
        }

        private static Texture2D? LoadThumbnailBytes(byte[] bytes)
        {
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes)) return texture;
            Object.Destroy(texture);
            ModLogger.LogWarning("YSM thumbnail could not be decoded by Unity.");
            return null;
        }

        private static Texture2D? LoadTextureFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ModLogger.LogError($"AssetBundleManager: Thumbnail file not found: {filePath}");
                return null;
            }

            try
            {
                var fileData = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData)) return texture;
                ModLogger.LogError($"AssetBundleManager: Failed to load image from file: {filePath}");
                Object.Destroy(texture);
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading texture from file '{filePath}'. Exception: {ex}");
                return null;
            }
        }

        private static async UniTask<Texture2D?> LoadTextureFromFileAsync(string filePath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                ModLogger.LogError($"AssetBundleManager: Thumbnail file not found: {filePath}");
                return null;
            }

            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                byte[] fileData;
                await using (var fileStream =
                             new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    fileData = new byte[fileStream.Length];
                    var offset = 0;
                    while (offset < fileData.Length)
                    {
                        var count = await fileStream.ReadAsync(fileData, offset, fileData.Length - offset,
                            cancellationToken).ConfigureAwait(false);
                        if (count == 0)
                            throw new EndOfStreamException(
                                $"Thumbnail ended after {offset} of {fileData.Length} bytes.");
                        offset += count;
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData)) return texture;
                ModLogger.LogError($"AssetBundleManager: Failed to load image from file: {filePath}");
                Object.Destroy(texture);
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ModLogger.LogError(
                    $"AssetBundleManager: Exception while loading texture from file '{filePath}'. Exception: {ex}");
                return null;
            }
        }

        public static T[]? LoadSpriteAtlases<T>(ModelBundleInfo bundleInfo) where T : Object
        {
            if (bundleInfo.SpriteAtlasPaths == null || bundleInfo.SpriteAtlasPaths.Length == 0)
                return null;

            var bundle = GetOrLoadAssetBundle(bundleInfo);
            if (bundle == null) return null;

            var atlases = new List<T>();
            foreach (var atlasPath in bundleInfo.SpriteAtlasPaths)
            {
                if (string.IsNullOrEmpty(atlasPath)) continue;

                try
                {
                    var atlas = bundle.LoadAsset<T>(atlasPath);
                    if (atlas != null)
                        atlases.Add(atlas);
                    else
                        ModLogger.LogWarning(
                            $"AssetBundleManager: Failed to load sprite atlas '{atlasPath}' from bundle '{bundleInfo.BundlePath}'");
                }
                catch (Exception ex)
                {
                    ModLogger.LogError(
                        $"AssetBundleManager: Exception while loading sprite atlas '{atlasPath}' from bundle '{bundleInfo.BundlePath}'. Exception: {ex}");
                }
            }

            return atlases.Count > 0 ? atlases.ToArray() : null;
        }

        public static async UniTask<T[]?> LoadSpriteAtlasesAsync<T>(ModelBundleInfo bundleInfo,
            CancellationToken cancellationToken = default) where T : Object
        {
            if (bundleInfo.SpriteAtlasPaths == null || bundleInfo.SpriteAtlasPaths.Length == 0)
                return null;

            var bundle = await GetOrLoadAssetBundleAsync(bundleInfo, false, cancellationToken);
            if (bundle == null) return null;

            var atlases = new List<T>();
            foreach (var atlasPath in bundleInfo.SpriteAtlasPaths)
            {
                if (string.IsNullOrEmpty(atlasPath)) continue;

                try
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    var atlas = bundle.LoadAsset<T>(atlasPath);
                    if (atlas != null)
                        atlases.Add(atlas);
                    else
                        ModLogger.LogWarning(
                            $"AssetBundleManager: Failed to load sprite atlas '{atlasPath}' from bundle '{bundleInfo.BundlePath}'");
                }
                catch (Exception ex)
                {
                    ModLogger.LogError(
                        $"AssetBundleManager: Exception while loading sprite atlas '{atlasPath}' from bundle '{bundleInfo.BundlePath}'. Exception: {ex}");
                }
            }

            return atlases.Count > 0 ? atlases.ToArray() : null;
        }
    }
}
