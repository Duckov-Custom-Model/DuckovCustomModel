using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Integrations;
using DuckovCustomModel.Integrations.Vrm;
using DuckovCustomModel.Integrations.Ysm;
using ModelRuntime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Managers
{
    public static class AssetBundleManager
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, BundleLoad> LoadingTasks = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<AssetBundle, HashSet<string>> AssetNames = [];

        private static string? GetBundlePath(ModelBundleInfo bundleInfo)
        {
            if (string.IsNullOrWhiteSpace(bundleInfo.BundlePath)) return null;
            var path = Path.GetFullPath(Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath));
            if (File.Exists(path)) return path;
            ModLogger.LogError($"AssetBundleManager: AssetBundle file not found at path: {path}");
            return null;
        }

        public static AssetBundle? GetOrLoadAssetBundle(ModelBundleInfo bundleInfo, bool forceReload = false)
        {
            var path = GetBundlePath(bundleInfo);
            if (path == null) return null;
            if (forceReload) UnloadAssetBundle(path);
            if (LoadedBundles.TryGetValue(path, out var existing) && existing != null) return existing;
            if (LoadingTasks.TryGetValue(path, out var pending))
            {
                var result = CompleteLoad(path, pending, pending.Request.assetBundle);
                if (!pending.Invalidated) return result;
            }

            try
            {
                var bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                    ModLogger.LogError($"AssetBundleManager: Failed to load AssetBundle from path: {path}");
                else LoadedBundles[path] = bundle;
                return bundle;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"AssetBundleManager: Failed to load '{path}': {ex}");
                return null;
            }
        }

        public static async UniTask<AssetBundle?> GetOrLoadAssetBundleAsync(ModelBundleInfo bundleInfo,
            bool forceReload = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = GetBundlePath(bundleInfo);
            if (path == null) return null;
            if (forceReload) UnloadAssetBundle(path);
            if (LoadedBundles.TryGetValue(path, out var existing) && existing != null) return existing;
            if (LoadingTasks.TryGetValue(path, out var obsolete) && obsolete.Invalidated)
            {
                await obsolete.Request.ToUniTask().AttachExternalCancellation(cancellationToken);
                CompleteLoad(path, obsolete, obsolete.Request.assetBundle);
                return await GetOrLoadAssetBundleAsync(bundleInfo, false, cancellationToken);
            }

            if (!LoadingTasks.TryGetValue(path, out var pending))
                try
                {
                    pending = new(AssetBundle.LoadFromFileAsync(path));
                    LoadingTasks.Add(path, pending);
                    FinishLoadAsync(path, pending).Forget();
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"AssetBundleManager: Failed to load '{path}': {ex}");
                    return null;
                }

            return await pending.Completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private static async UniTask FinishLoadAsync(string path, BundleLoad pending)
        {
            try
            {
                await pending.Request.ToUniTask();
                CompleteLoad(path, pending, pending.Request.assetBundle);
            }
            catch (Exception ex)
            {
                if (LoadingTasks.TryGetValue(path, out var current) && ReferenceEquals(current, pending))
                    LoadingTasks.Remove(path);
                pending.Completion.TrySetException(ex);
                ModLogger.LogError($"AssetBundleManager: Failed to load '{path}': {ex}");
            }
        }

        private static AssetBundle? CompleteLoad(string path, BundleLoad pending, AssetBundle? bundle)
        {
            if (pending.Completed) return pending.Result;
            pending.Completed = true;
            var current = LoadingTasks.TryGetValue(path, out var active) && ReferenceEquals(active, pending);
            if (current) LoadingTasks.Remove(path);
            if (pending.Invalidated || !current)
            {
                if (bundle != null) bundle.Unload(true);
                pending.Completion.TrySetCanceled();
                return null;
            }

            if (bundle != null) LoadedBundles[path] = bundle;
            else ModLogger.LogError($"AssetBundleManager: Failed to load AssetBundle from path: {path}");
            pending.Result = bundle;
            pending.Completion.TrySetResult(bundle);
            return bundle;
        }

        public static void UnloadAssetBundle(string bundlePath)
        {
            if (string.IsNullOrWhiteSpace(bundlePath)) return;
            bundlePath = Path.GetFullPath(bundlePath);
            if (LoadingTasks.TryGetValue(bundlePath, out var pending))
            {
                pending.Invalidated = true;
                pending.Completion.TrySetCanceled();
            }

            if (!LoadedBundles.TryGetValue(bundlePath, out var bundle)) return;
            LoadedBundles.Remove(bundlePath);
            AssetNames.Remove(bundle);
            if (bundle != null) bundle.Unload(true);
        }

        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var pending in new List<BundleLoad>(LoadingTasks.Values))
            {
                pending.Invalidated = true;
                pending.Completion.TrySetCanceled();
            }

            var bundles = new List<AssetBundle>(LoadedBundles.Values);
            LoadedBundles.Clear();
            AssetNames.Clear();
            foreach (var bundle in bundles)
                if (bundle != null)
                    bundle.Unload(unloadAllLoadedObjects);
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

        internal static string? FindBinaryAssetPath(ModelBundleInfo bundleInfo, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(bundleInfo.BundlePath)) return null;
            if (!File.Exists(Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath))) return null;
            var bundle = GetOrLoadAssetBundle(bundleInfo);
            if (bundle == null) return null;
            var normalized = assetPath.Replace('\\', '/').TrimStart('/');
            if (!AssetNames.TryGetValue(bundle, out var names))
            {
                names = new(bundle.GetAllAssetNames(), StringComparer.OrdinalIgnoreCase);
                AssetNames.Add(bundle, names);
            }

            var candidates = new List<string>();
            if (ModelSourcePaths.IsFileSource(bundleInfo))
            {
                var bundleDirectory = Path.GetDirectoryName(
                    Path.GetFullPath(Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath)))!;
                var prefix = Path.GetRelativePath(bundleInfo.DirectoryPath, bundleDirectory).Replace('\\', '/');
                if (prefix != "." && normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                {
                    var relative = normalized.Substring(prefix.Length + 1);
                    candidates.Add(relative);
                    candidates.Add(relative + ".bytes");
                }
            }

            candidates.Add(normalized);
            candidates.Add(normalized + ".bytes");

            foreach (var candidate in candidates)
            {
                if (!names.Contains(candidate)) continue;
                return candidate;
            }

            return null;
        }

        internal static byte[] LoadBinaryBytes(ModelBundleInfo bundleInfo, string assetPath)
        {
            var bundle = GetOrLoadAssetBundle(bundleInfo)
                         ?? throw new FileNotFoundException("Model AssetBundle could not be loaded.",
                             bundleInfo.BundlePath);
            var asset = bundle.LoadAsset<TextAsset>(assetPath);
            if (asset == null)
                throw new InvalidDataException("Embedded model must be a TextAsset: " + assetPath);
            var bytes = asset.bytes;
            if (bytes.Length > new LoadLimits().MaxFileBytes)
                throw new InvalidDataException("Embedded model exceeds the loading budget: " + assetPath);
            return bytes;
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
                if (embedded != null || (!YsmModelSource.IsYsm(modelInfo) && !VrmModelSource.IsVrm(modelInfo)))
                    return embedded;
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
            if (YsmModelSource.IsYsm(modelInfo) || VrmModelSource.IsVrm(modelInfo))
                try
                {
                    return (VrmModelSource.IsVrm(modelInfo)
                        ? VrmModelSource.HasSource(bundleInfo, modelInfo)
                        : YsmModelSource.HasSource(bundleInfo, modelInfo))
                        ? (true, null)
                        : (false, "Model source was not found");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }

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
            if (YsmModelSource.IsYsm(modelInfo) || VrmModelSource.IsVrm(modelInfo))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return (VrmModelSource.IsVrm(modelInfo)
                        ? VrmModelSource.HasSource(bundleInfo, modelInfo)
                        : YsmModelSource.HasSource(bundleInfo, modelInfo))
                        ? (true, null)
                        : (false, "Model source was not found");
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

        private sealed class BundleLoad
        {
            internal readonly UniTaskCompletionSource<AssetBundle?> Completion = new();
            internal readonly AssetBundleCreateRequest Request;
            internal bool Invalidated, Completed;
            internal AssetBundle? Result;

            internal BundleLoad(AssetBundleCreateRequest request)
            {
                Request = request;
            }
        }
    }
}
