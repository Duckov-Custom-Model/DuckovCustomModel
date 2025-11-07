using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DuckovCustomModel.Data;
using DuckovCustomModel.MonoBehaviours;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Managers
{
    public static class ModelManager
    {
        public static readonly List<ModelBundleInfo> ModelBundles = [];

        private static readonly Dictionary<string, BundleHashInfo> BundleHashCache = [];

        private static string HashCacheFilePath =>
            Path.Combine(ConfigManager.ConfigBaseDirectory, "bundle_hash_cache.json");

        public static string ModelsDirectory => Path.Combine(ConfigManager.ConfigBaseDirectory, "Models");

        public static HashSet<string> UpdateModelBundles()
        {
            LoadHashCache();

            if (!Directory.Exists(ModelsDirectory))
                Directory.CreateDirectory(ModelsDirectory);

            var modelBundleDirectories = Directory.GetDirectories(ModelsDirectory);
            var bundlesToUnload = new HashSet<string>();
            var bundlesToReload = new HashSet<string>();

            foreach (var modelBundleDir in modelBundleDirectories)
            {
                var infoFilePath = Path.Combine(modelBundleDir, "bundleinfo.json");
                if (!File.Exists(infoFilePath)) continue;

                var bundleInfo = ModelBundleInfo.LoadFromDirectory(modelBundleDir);
                if (bundleInfo == null) continue;

                var bundlePath = Path.Combine(bundleInfo.DirectoryPath, bundleInfo.BundlePath);
                if (!File.Exists(bundlePath)) continue;

                var configContent = File.ReadAllText(infoFilePath);
                var configHash = BundleHashInfo.CalculateStringHash(configContent);
                var bundleHash = BundleHashInfo.CalculateFileHash(bundlePath);

                var bundleKey = bundleInfo.BundleName;
                var needsReload = false;

                if (BundleHashCache.TryGetValue(bundleKey, out var cachedHash))
                {
                    if (cachedHash.ConfigHash != configHash || cachedHash.BundleHash != bundleHash)
                    {
                        needsReload = true;
                        ModLogger.Log($"Bundle '{bundleKey}' changed, will reload");
                    }
                }
                else
                {
                    needsReload = true;
                    ModLogger.Log($"New bundle '{bundleKey}' detected, will load");
                }

                if (needsReload)
                {
                    bundlesToReload.Add(bundleKey);
                    bundlesToUnload.Add(bundleKey);
                }

                var hashInfo = new BundleHashInfo
                {
                    BundleName = bundleKey,
                    BundlePath = bundleInfo.BundlePath,
                    BundleHash = bundleHash,
                    ConfigHash = configHash,
                    LastModified = File.GetLastWriteTime(bundlePath),
                };
                BundleHashCache[bundleKey] = hashInfo;
            }

            var existingBundles = ModelBundles.ToList();
            foreach (var existingBundle in existingBundles)
            {
                var bundleKey = existingBundle.BundleName;
                var bundleDir = Path.Combine(ModelsDirectory, bundleKey);
                if (!Directory.Exists(bundleDir))
                {
                    bundlesToUnload.Add(bundleKey);
                    ModelBundles.Remove(existingBundle);
                    BundleHashCache.Remove(bundleKey);
                    ModLogger.Log($"Bundle '{bundleKey}' removed");
                }
            }

            foreach (var bundleKey in bundlesToUnload)
            {
                var bundleToUnload = ModelBundles.FirstOrDefault(b => b.BundleName == bundleKey);
                if (bundleToUnload != null)
                {
                    foreach (ModelTarget target in Enum.GetValues(typeof(ModelTarget)))
                    {
                        var handlers = GetAllModelHandlers(target);
                        foreach (var handler in handlers)
                        {
                            if (!handler.IsHiddenOriginalModel) continue;
                            var modelID = ModBehaviour.Instance?.UsingModel?.GetModelID(target) ?? string.Empty;
                            if (string.IsNullOrEmpty(modelID)) continue;
                            if (!FindModelByID(modelID, out var currentBundleInfo, out _)) continue;
                            if (currentBundleInfo.BundleName == bundleKey)
                                handler.CleanupCustomModel();
                        }
                    }

                    var bundlePath = Path.Combine(bundleToUnload.DirectoryPath, bundleToUnload.BundlePath);
                    AssetBundleManager.UnloadAssetBundle(bundlePath);
                }
            }

            foreach (var modelBundleDir in modelBundleDirectories)
            {
                var bundleInfo = ModelBundleInfo.LoadFromDirectory(modelBundleDir);
                if (bundleInfo == null) continue;

                var bundleKey = bundleInfo.BundleName;
                if (bundlesToReload.Contains(bundleKey))
                {
                    var existingBundle = ModelBundles.FirstOrDefault(b => b.BundleName == bundleKey);
                    if (existingBundle != null)
                        ModelBundles.Remove(existingBundle);

                    foreach (var modelInfo in bundleInfo.Models)
                        modelInfo.BundleName = bundleKey;

                    ModelBundles.Add(bundleInfo);
                }
                else
                {
                    var existingBundle = ModelBundles.FirstOrDefault(b => b.BundleName == bundleKey);
                    if (existingBundle == null)
                    {
                        foreach (var modelInfo in bundleInfo.Models)
                            modelInfo.BundleName = bundleKey;

                        ModelBundles.Add(bundleInfo);
                    }
                    else
                    {
                        foreach (var modelInfo in existingBundle.Models)
                            modelInfo.BundleName = bundleKey;
                    }
                }
            }

            SaveHashCache();
            CheckDuplicateModelIDs();

            return bundlesToReload;
        }

        public static IReadOnlyDictionary<string, List<string>> GetDuplicateModelIDs()
        {
            var modelIdDictionary = new Dictionary<string, List<string>>();
            foreach (var modelBundle in ModelBundles)
            foreach (var modelInfo in modelBundle.Models)
            {
                if (!modelIdDictionary.ContainsKey(modelInfo.ModelID))
                    modelIdDictionary[modelInfo.ModelID] = [];
                modelIdDictionary[modelInfo.ModelID].Add(modelBundle.BundleName);
            }

            return modelIdDictionary.Where(kvp => kvp.Value.Count > 1)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static bool FindModelByID(string modelID,
            [NotNullWhen(true)] out ModelBundleInfo? foundModel,
            [NotNullWhen(true)] out ModelInfo? foundModelInfo)
        {
            foundModel = null;
            foundModelInfo = null;

            foreach (var modelBundle in ModelBundles)
            foreach (var modelInfo in modelBundle.Models)
                if (modelInfo.ModelID == modelID)
                {
                    foundModel = modelBundle;
                    foundModelInfo = modelInfo;
                    return true;
                }

            return false;
        }

        public static ModelHandler? InitializeModelHandler(CharacterMainControl characterMainControl,
            ModelTarget target = ModelTarget.Character)
        {
            if (characterMainControl == null)
            {
                ModLogger.LogError("CharacterMainControl is null.");
                return null;
            }

            var modelHandler = characterMainControl.GetComponent<ModelHandler>();
            if (modelHandler == null)
                modelHandler = characterMainControl.gameObject.AddComponent<ModelHandler>();

            modelHandler.Initialize(characterMainControl, target);

            return modelHandler;
        }

        public static List<ModelHandler> GetAllModelHandlers(ModelTarget target)
        {
            var handlers = new List<ModelHandler>();

            if (LevelManager.Instance == null) return handlers;

            var allHandlers = Object.FindObjectsByType<ModelHandler>(FindObjectsSortMode.None);
            handlers.AddRange(allHandlers.Where(handler => handler.Target == target && handler.IsInitialized));

            return handlers;
        }

        private static void LoadHashCache()
        {
            BundleHashCache.Clear();
            if (!File.Exists(HashCacheFilePath)) return;

            try
            {
                var json = File.ReadAllText(HashCacheFilePath);
                var cache = JsonConvert.DeserializeObject<Dictionary<string, BundleHashInfo>>(json,
                    Constant.JsonSettings);
                if (cache != null)
                    foreach (var kvp in cache)
                        BundleHashCache[kvp.Key] = kvp.Value;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load hash cache: {ex.Message}");
            }
        }

        private static void SaveHashCache()
        {
            try
            {
                var json = JsonConvert.SerializeObject(BundleHashCache, Constant.JsonSettings);
                File.WriteAllText(HashCacheFilePath, json);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to save hash cache: {ex.Message}");
            }
        }

        private static void CheckDuplicateModelIDs()
        {
            var duplicates = GetDuplicateModelIDs();
            foreach (var (modelID, bundles) in duplicates)
            {
                var bundleNames = string.Join(", ", bundles);
                ModLogger.LogWarning($"Duplicate ModelID '{modelID}' found in bundles: {bundleNames}");
            }
        }
    }
}