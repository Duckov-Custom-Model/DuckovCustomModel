using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UniGLTF;
using UnityEngine;
using UniVRM10;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal static class VrmModelSource
    {
        private static readonly SemaphoreSlim ImportSlots = new(2, 2);
        private static readonly Dictionary<string, Task> PendingTemplates = new(StringComparer.Ordinal);
        private static CancellationTokenSource _templateCancellation = new();
        private static bool _registered;
        private static AssetBundle? _resources;
        private static int _loadingCount;
        private static bool _clearResourcesPending;

        internal static bool IsVrm(ModelInfo model)
        {
            return string.Equals(model.SourceKind, "Vrm", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsVrm(ModelBundleInfo bundle)
        {
            return string.Equals(bundle.SourceKind, "Vrm", StringComparison.OrdinalIgnoreCase);
        }

        internal static void Register()
        {
            if (_registered) return;
            ModelSourceLoader.Register(new("Vrm", false), Load);
            _registered = true;
        }

        internal static void ClearResources()
        {
            ClearTemplates();
            VrmResourcePool.Clear();
            _clearResourcesPending = true;
            if (_loadingCount != 0) return;
            if (_resources != null) _resources.Unload(true);
            _resources = null;
            _clearResourcesPending = false;
        }

        private static T Resource<T>(string path) where T : Object
        {
            if (_resources == null)
                _resources =
                    AssetBundle.LoadFromFile(Path.Combine(ModEntry.ModDirectory!, "Resources", "vrm-runtime.bundle"));
            if (_resources == null) throw new FileNotFoundException("VRM runtime resources could not be loaded.");
            return _resources.LoadAsset<T>(path) ?? throw new InvalidDataException("VRM resource missing: " + path);
        }

        internal static IMaterialDescriptorGenerator CreateAnimationMaterialGenerator()
        {
            var generator = new UrpGltfMaterialDescriptorGenerator();
            generator.DefaultMaterialImporter.Shader =
                Resource<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader");
            return generator;
        }

        internal static bool HasSource(ModelBundleInfo bundle, ModelInfo model)
        {
            return AssetBundleManager.FindBinaryAssetPath(bundle, model.SourcePath) != null ||
                   File.Exists(ModelSourcePaths.SourcePath(bundle, model));
        }

        internal static void ClearTemplates()
        {
            var previous = _templateCancellation;
            _templateCancellation = new();
            previous.Cancel();
            previous.Dispose();
            PendingTemplates.Clear();
            VrmTemplateCache.Clear();
        }

        private static string TemplateKey(ModelBundleInfo bundle, ModelInfo model)
        {
            var embedded = AssetBundleManager.FindBinaryAssetPath(bundle, model.SourcePath);
            var path = embedded == null
                ? ModelSourcePaths.SourcePath(bundle, model)
                : Path.Combine(bundle.DirectoryPath, bundle.BundlePath);
            var file = new FileInfo(path);
            return path + "|" + embedded + "|" + model.SourceRevision + "|" + file.Length + "|" +
                   file.LastWriteTimeUtc.Ticks;
        }

        internal static void SetPreferredModels(IEnumerable<(ModelBundleInfo bundle, ModelInfo model)> models)
        {
            var keys = new List<string>();
            foreach (var (bundle, model) in models)
            {
                if (!IsVrm(model)) continue;
                try
                {
                    keys.Add(TemplateKey(bundle, model));
                }
                catch (IOException)
                {
                }
            }

            VrmTemplateCache.SetPinnedKeys(keys);
        }

        internal static async UniTask Preload(ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken, bool speculative = false)
        {
            if (!IsVrm(model) || (speculative && PendingTemplates.Count >= 2)) return;
            var key = TemplateKey(bundle, model);
            if (VrmTemplateCache.Contains(key)) return;
            await WaitForTemplate(PrepareTemplate(key, bundle, model)).AttachExternalCancellation(cancellationToken);
        }

        private static async UniTask WaitForTemplate(Task task)
        {
            await task;
        }

        private static Task PrepareTemplate(string key, ModelBundleInfo bundle, ModelInfo model)
        {
            if (PendingTemplates.TryGetValue(key, out var pending)) return pending;
            var task = ImportTemplate(key, bundle, model, _templateCancellation.Token).AsTask();
            PendingTemplates.Add(key, task);
            return task;
        }

        private static async UniTask ImportTemplate(string key, ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            await UniTask.Yield();
            try
            {
                await ImportSlots.WaitAsync(cancellationToken);
                try
                {
                    var root = await LoadCore(bundle, model, cancellationToken);
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        VrmTemplateCache.Store(key, root);
                    }
                    catch
                    {
                        DestroyImported(root);
                        throw;
                    }
                }
                finally
                {
                    ImportSlots.Release();
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested) PendingTemplates.Remove(key);
            }
        }

        private static async UniTask<PreparedModel> Load(ModelHandler handler, ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();
            var key = TemplateKey(bundle, model);
            if (!VrmTemplateCache.TryCreate(key, out var root))
            {
                await WaitForTemplate(PrepareTemplate(key, bundle, model))
                    .AttachExternalCancellation(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!VrmTemplateCache.TryCreate(key, out root))
                    throw new InvalidOperationException("Prepared VRM template is unavailable.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var instance = root.GetComponent<Vrm10Instance>();
                root.transform.SetParent(handler.OriginalCharacterModel!.transform, false);
                root.transform.localPosition = Vector3.zero;
                var settings = model.Vrm ?? new();
                root.transform.localRotation = Quaternion.Euler(0, settings.RotationY, 0);
                root.transform.localScale = Vector3.one * settings.Scale;
                var animator = root.GetComponent<Animator>();
                if (animator == null || !animator.isHuman)
                    throw new InvalidDataException("VRM humanoid Animator is invalid.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.runtimeAnimatorController = string.IsNullOrWhiteSpace(settings.AnimatorControllerPath)
                    ? Resource<RuntimeAnimatorController>("Assets/Generated/VrmLocomotion.controller")
                    : AssetBundleManager.LoadAssetFromBundle<RuntimeAnimatorController>(bundle,
                          settings.AnimatorControllerPath)
                      ?? throw new InvalidDataException("Configured VRM AnimatorController could not be loaded.");
                root.AddComponent<VrmCharacterRuntime>().Initialize(handler, instance, settings);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetFloat("_DcmVrmInstance", 1);
                    renderer.SetPropertyBlock(block);
                }

                ModLogger.Log($"VRM instance '{model.ModelID}': total={timer.ElapsedMilliseconds}ms, " +
                              $"springs={instance.SpringBone.Springs.Count}, " +
                              $"joints={instance.SpringBone.Springs.Sum(spring => spring.Joints.Count)}");
                return new(root);
            }
            catch
            {
                if (root != null) Object.DestroyImmediate(root);
                throw;
            }
        }

        private static async UniTask<GameObject> LoadCore(ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();
            var embedded = AssetBundleManager.FindBinaryAssetPath(bundle, model.SourcePath);
            var embeddedBytes = embedded == null ? null : AssetBundleManager.LoadBinaryBytes(bundle, embedded);
            var embeddedReadMilliseconds = timer.ElapsedMilliseconds;
            var preparation = await Task.Run(() =>
            {
                var workerTimer = Stopwatch.StartNew();
                var bytes = embeddedBytes ?? VrmMetadata.ReadFile(ModelSourcePaths.SourcePath(bundle, model));
                var readMilliseconds = workerTimer.ElapsedMilliseconds + embeddedReadMilliseconds;
                cancellationToken.ThrowIfCancellationRequested();
                var validationStart = workerTimer.ElapsedMilliseconds;
                VrmMetadata.Read(bytes, false);
                var metadataMilliseconds = workerTimer.ElapsedMilliseconds - validationStart;
                var key = Convert.ToBase64String(VrmMetadata.HashContent(bytes));
                var hashMilliseconds = workerTimer.ElapsedMilliseconds - validationStart - metadataMilliseconds;
                cancellationToken.ThrowIfCancellationRequested();
                var parseStart = workerTimer.ElapsedMilliseconds;
                var vrmData = ParseData(bytes);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return (Vrm: vrmData, Key: key, ReadMilliseconds: readMilliseconds,
                        MetadataMilliseconds: metadataMilliseconds, HashMilliseconds: hashMilliseconds,
                        ParseMilliseconds: workerTimer.ElapsedMilliseconds - parseStart);
                }
                catch
                {
                    vrmData.Data.Dispose();
                    throw;
                }
            }, cancellationToken);
            using var data = preparation.Vrm.Data;
            await UniTask.SwitchToMainThread(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var preparedMilliseconds = timer.ElapsedMilliseconds;
            var validationMilliseconds = preparation.MetadataMilliseconds + preparation.HashMilliseconds;
            var generator = new UrpVrm10MaterialDescriptorGenerator();
            generator.MToonMaterialImporter.Shader =
                Resource<Shader>("Packages/com.vrmc.vrm/MToon10/Shaders/vrmc_materials_mtoon_urp.shader");
            generator.PbrMaterialImporter.Shader =
                Resource<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader");
            generator.DefaultMaterialImporter.Shader = generator.PbrMaterialImporter.Shader;
            generator.UnlitMaterialImporter.Shader =
                Resource<Shader>("Packages/com.vrmc.gltf/UniUnlit/Shaders/UniUnlit.shader");
            Vrm10Instance? instance = null;
            var entry = VrmResourcePool.Acquire(preparation.Key);
            var gateHeld = false;
            var importEntry = entry;
            _loadingCount++;
            try
            {
                var gateStart = timer.ElapsedMilliseconds;
                await entry.Gate.WaitAsync(cancellationToken);
                var gateMilliseconds = timer.ElapsedMilliseconds - gateStart;
                gateHeld = true;
                cancellationToken.ThrowIfCancellationRequested();
                using var importer = new Vrm10Importer(preparation.Vrm, entry.Textures, materialGenerator: generator,
                    useControlRig: false);
                var importStart = timer.ElapsedMilliseconds;
                var importTimings = new ImporterContextSpeedLog();
                var gltf = await importer.LoadAsync(new VrmAwaitCaller(), importTimings.MeasureTime);
                var importMilliseconds = timer.ElapsedMilliseconds - importStart;
                instance = gltf.GetComponent<Vrm10Instance>();
                if (instance == null)
                {
                    Object.DestroyImmediate(gltf.gameObject);
                    throw new InvalidDataException("VRM importer returned no instance.");
                }

                var owner = gltf.gameObject.AddComponent<VrmResourceOwner>();
                owner.Entry = entry;
                entry = null;
                gltf.TransferOwnership((key, resource) =>
                {
                    if (resource is Texture && !importEntry.Textures.ContainsKey(key))
                        importEntry.Textures.Add(key, resource);
                    else owner.Resources.Add(resource);
                });
                importEntry.Gate.Release();
                gateHeld = false;
                cancellationToken.ThrowIfCancellationRequested();
                if (instance == null) throw new InvalidDataException("VRM importer returned no instance.");
                var root = instance.gameObject;
                root.SetActive(false);
                root.name = ModelHandler.CustomModelInstanceName;
                instance.GetComponent<RuntimeGltfInstance>().ShowMeshes();
                var stages = string.Join(", ", importTimings.m_speedReports.GroupBy(report => report.Key)
                    .Select(group => $"{group.Key}={group.Sum(report => report.Elapsed.TotalMilliseconds):F0}ms"));
                ModLogger.Log($"VRM loaded '{model.ModelID}': total={timer.ElapsedMilliseconds}ms, " +
                              $"springs={instance.SpringBone.Springs.Count}, " +
                              $"joints={instance.SpringBone.Springs.Sum(spring => spring.Joints.Count)}, " +
                              $"read={preparation.ReadMilliseconds}ms, validate={validationMilliseconds}ms, " +
                              $"metadata={preparation.MetadataMilliseconds}ms, hash={preparation.HashMilliseconds}ms, " +
                              $"prepare={preparedMilliseconds}ms, " +
                              $"resume={Math.Max(0, preparedMilliseconds - preparation.ReadMilliseconds - validationMilliseconds - preparation.ParseMilliseconds)}ms, " +
                              $"resources={gateStart - preparedMilliseconds}ms, " +
                              $"gate={gateMilliseconds}ms, parse={preparation.ParseMilliseconds}ms, " +
                              $"import={importMilliseconds}ms, " +
                              $"setup={timer.ElapsedMilliseconds - importStart - importMilliseconds}ms, {stages}");
                return root;
            }
            catch
            {
                if (gateHeld)
                {
                    importEntry.Gate.Release();
                    gateHeld = false;
                }

                if (instance != null) DestroyImported(instance.gameObject);
                throw;
            }
            finally
            {
                if (gateHeld) importEntry.Gate.Release();
                if (entry != null) VrmResourcePool.Release(entry);
                _loadingCount--;
                if (_clearResourcesPending && _loadingCount == 0)
                {
                    if (_resources != null) _resources.Unload(true);
                    _resources = null;
                    _clearResourcesPending = false;
                }
            }
        }

        private static void DestroyImported(GameObject root)
        {
            if (root == null) return;
            root.GetComponent<Vrm10Instance>()?.DisposeRuntime();
            root.GetComponent<VrmResourceOwner>()?.Release();
            Object.DestroyImmediate(root);
        }

        private static Vrm10Data ParseData(byte[] bytes)
        {
            var data = GlbLowLevelParser.Parse("", bytes);
            try
            {
                var parsed = Vrm10Data.Parse(data);
                if (parsed != null) return parsed;
                var migrated = Vrm10Data.Migrate(data, out var vrm, out _);
                if (vrm == null)
                {
                    migrated?.Dispose();
                    throw new InvalidDataException("VRM migration failed.");
                }

                data.Dispose();
                return vrm;
            }
            catch
            {
                data.Dispose();
                throw;
            }
        }

        internal static async UniTask<HashSet<string>> RefreshAsync(string directory, List<ModelBundleInfo> bundles,
            CancellationToken cancellationToken)
        {
            var previous = bundles.Where(IsVrm).ToDictionary(b => b.BundleName, StringComparer.Ordinal);
            var found = await Task.Run(() => Discover(directory, previous, cancellationToken), cancellationToken);
            await UniTask.SwitchToMainThread(cancellationToken);
            var changed = new HashSet<string>(previous.Keys, StringComparer.Ordinal);
            foreach (var bundle in found)
                if (previous.TryGetValue(bundle.BundleName, out var old) && ReferenceEquals(old, bundle))
                    changed.Remove(bundle.BundleName);
                else changed.Add(bundle.BundleName);
            bundles.RemoveAll(IsVrm);
            bundles.AddRange(found);
            return changed;
        }

        private static List<ModelBundleInfo> Discover(string directory, Dictionary<string, ModelBundleInfo> previous,
            CancellationToken cancellationToken)
        {
            var result = new List<ModelBundleInfo>();
            Scan(directory, 0);
            return result;

            void Scan(string folder, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    ModelSourcePaths.RejectLinks(folder);
                    if (depth > 8 || (depth > 0 && File.Exists(Path.Combine(folder, "bundleinfo.json")))) return;
                    foreach (var file in Directory.EnumerateFiles(folder, "*.vrm"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
                        var key = "VRM/" + relative;
                        try
                        {
                            var bytes = VrmMetadata.ReadFile(file);
                            var metadata = VrmMetadata.Read(bytes);
                            var sidecar = file + ".duckov.json";
                            var json = File.Exists(sidecar)
                                ? File.ReadAllText(ModelSourcePaths.Resolve(folder, Path.GetFileName(sidecar)))
                                : "{}";
                            var profile = JObject.Parse(json);
                            var model = JsonConvert.DeserializeObject<ModelInfo>(json, JsonSettings.Default) ?? new();
                            model.SourceKind = "Vrm";
                            model.SourcePath = relative;
                            model.BundleName = key;
                            if (string.IsNullOrWhiteSpace(model.ModelID)) model.ModelID = "vrm:" + relative;
                            if (string.IsNullOrWhiteSpace(model.Name))
                                model.Name = string.IsNullOrWhiteSpace(metadata.Name)
                                    ? Path.GetFileNameWithoutExtension(file)
                                    : metadata.Name;
                            if (string.IsNullOrWhiteSpace(model.Author)) model.Author = metadata.Author;
                            if (string.IsNullOrWhiteSpace(model.Version)) model.Version = metadata.Version;
                            if (model.TargetTypes.Length == 0)
                                model.TargetTypes = [ModelTargetType.Character, ModelTargetType.AllAICharacters];
                            model.ThumbnailData = metadata.Thumbnail;
                            if (!string.IsNullOrWhiteSpace(model.ThumbnailPath))
                            {
                                model.ThumbnailPath = ModelSourcePaths.Resolve(folder, model.ThumbnailPath);
                                model.ThumbnailData = null;
                            }

                            using var digest = SHA256.Create();
                            if (!model.Validate()) throw new InvalidDataException("Invalid VRM model settings.");
                            var bundle = ModelBundleInfo.CreateSourceBundle(directory, key, "Vrm", [model]);
                            var bundlePath = (string?)profile["BundlePath"];
                            if (!string.IsNullOrWhiteSpace(bundlePath))
                                bundle.BundlePath = Path.GetRelativePath(directory,
                                    ModelSourcePaths.Resolve(folder, bundlePath));
                            var revision = Convert.ToBase64String(VrmMetadata.HashContent(bytes)) + ":" + json;
                            foreach (var dependency in new[]
                                     {
                                         model.ThumbnailPath,
                                         string.IsNullOrWhiteSpace(bundlePath)
                                             ? null
                                             : ModelSourcePaths.Resolve(folder, bundlePath),
                                     })
                            {
                                if (string.IsNullOrWhiteSpace(dependency) || !File.Exists(dependency)) continue;
                                using var stream = File.OpenRead(dependency);
                                revision += ":" + Convert.ToBase64String(digest.ComputeHash(stream));
                            }

                            model.SourceRevision =
                                Convert.ToBase64String(digest.ComputeHash(Encoding.UTF8.GetBytes(revision)));
                            if (previous.TryGetValue(key, out var old) &&
                                old.Models[0].SourceRevision == model.SourceRevision &&
                                ModelLoadStatus.GetError(old.Models[0]) == null)
                            {
                                result.Add(old);
                            }
                            else
                            {
                                if (old != null)
                                    foreach (var previousModel in old.Models)
                                        ModelLoadStatus.Succeeded(previousModel, previousModel.SourceRevision);
                                result.Add(bundle);
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            ModLogger.LogWarning($"VRM source '{relative}' could not be refreshed: {ex.Message}");
                            if (!previous.TryGetValue(key, out var old))
                            {
                                var failedModel = new ModelInfo
                                {
                                    ModelID = "vrm:" + relative, Name = Path.GetFileNameWithoutExtension(file),
                                    SourceKind = "Vrm", SourcePath = relative, BundleName = key,
                                    TargetTypes = [ModelTargetType.Character, ModelTargetType.AllAICharacters],
                                };
                                old = ModelBundleInfo.CreateSourceBundle(directory, key, "Vrm", [failedModel]);
                            }

                            foreach (var failedModel in old.Models)
                                ModelLoadStatus.Failed(failedModel, failedModel.SourceRevision, ex);
                            result.Add(old);
                        }
                    }

                    foreach (var child in Directory.EnumerateDirectories(folder)) Scan(child, depth + 1);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ModLogger.LogWarning($"VRM discovery skipped '{folder}': {ex.Message}");
                    var prefix = "VRM/" + Path.GetRelativePath(directory, folder).Replace('\\', '/').TrimEnd('/') + "/";
                    foreach (var pair in previous)
                        if ((folder == directory || pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) &&
                            result.All(bundle => bundle.BundleName != pair.Key))
                            result.Add(pair.Value);
                }
            }
        }
    }
}
