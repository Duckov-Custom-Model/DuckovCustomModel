using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using ModelRuntime;
using ModelRuntime.Json;
using ModelRuntime.Ysm;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class YsmModelLease : IDisposable
    {
        private Action? release;

        internal YsmModelLease(ModelPackage package, DuckovYsmBindingProfile profile, string sourcePath, Action release)
        {
            Package = package;
            Profile = profile;
            SourcePath = sourcePath;
            this.release = release;
        }

        public ModelPackage Package { get; }
        public DuckovYsmBindingProfile Profile { get; }
        public string SourcePath { get; }

        public void Dispose()
        {
            var callback = release;
            release = null;
            callback?.Invoke();
        }
    }

    public static class YsmModelSource
    {
        private const int MaxIdlePackages = 4;
        private static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConditionalWeakTable<ModelDocument, HashSet<string>> OriginalAnimations = new();
        private static readonly object CacheLock = new();
        private static long cacheUseSequence;

        private static readonly JsonSerializerSettings ProfileSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None, MaxDepth = 32,
            Converters = [new StringEnumConverter()],
        };

        public static bool IsYsm(ModelInfo? model)
        {
            return string.Equals(model?.SourceKind, "Ysm", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsYsm(ModelBundleInfo? bundle)
        {
            return string.Equals(bundle?.SourceKind, "Ysm", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsFallbackAnimation(ModelDocument document, string animation)
        {
            return OriginalAnimations.TryGetValue(document, out var names) && !names.Contains(animation)
                   && document.Animations.ContainsKey(animation);
        }

        public static string GetSourcePath(ModelBundleInfo bundle, ModelInfo model)
        {
            if (!IsYsm(model)) throw new ArgumentException("Model is not a YSM source.");
            var directory = IsYsm(bundle) || string.IsNullOrWhiteSpace(bundle.BundlePath)
                ? bundle.DirectoryPath
                : Path.GetDirectoryName(Path.GetFullPath(Path.Combine(bundle.DirectoryPath, bundle.BundlePath)))!;
            return ResolveInside(directory, model.SourcePath);
        }

        public static bool HasSource(ModelBundleInfo bundle, ModelInfo model)
        {
            if (!IsYsm(model)) return false;
            return AssetBundleManager.FindYsmAssetPath(bundle, model.SourcePath) != null
                   || File.Exists(GetSourcePath(bundle, model)) || Directory.Exists(GetSourcePath(bundle, model));
        }

        public static ModelPackage Load(ModelBundleInfo bundle, ModelInfo model)
        {
            var path = GetSourcePath(bundle, model);
            var profile = ReadProfile(path);
            var assetPath = AssetBundleManager.FindYsmAssetPath(bundle, model.SourcePath);
            return LoadValidated(path, profile,
                assetPath == null ? null : AssetBundleManager.LoadYsmBytes(bundle, assetPath));
        }

        public static YsmModelLease Acquire(ModelBundleInfo bundle, ModelInfo model)
        {
            var path = GetSourcePath(bundle, model);
            var assetPath = AssetBundleManager.FindYsmAssetPath(bundle, model.SourcePath);
            var revision = assetPath == null ? Fingerprint(path) : IsYsm(bundle)
                ? Fingerprint(path) + "|" + BundleRevision(bundle, assetPath, path)
                : BundleRevision(bundle, assetPath, path);
            if (!string.IsNullOrEmpty(model.SourceRevision) && revision != model.SourceRevision)
                throw new IOException("YSM source changed since discovery; refresh the model list before loading.");
            var profile = ReadProfile(path);
            var key = (assetPath == null ? path : bundle.BundlePath + "|" + assetPath) + "|" + revision;
            lock (CacheLock)
            {
                if (!Cache.TryGetValue(key, out var entry))
                {
                    var package = LoadValidated(path, profile,
                        assetPath == null ? null : AssetBundleManager.LoadYsmBytes(bundle, assetPath));
                    if ((assetPath == null ? Fingerprint(path) : IsYsm(bundle)
                            ? Fingerprint(path) + "|" + BundleRevision(bundle, assetPath, path)
                            : BundleRevision(bundle, assetPath, path)) != revision)
                        throw new IOException("YSM source changed while loading.");
                    entry = new() { Package = package };
                    Cache.Add(key, entry);
                }

                entry.LastUsed = ++cacheUseSequence;
                entry.References++;
                return new(entry.Package, profile, path, () =>
                {
                    lock (CacheLock)
                    {
                        entry.References--;
                        TrimIdlePackages();
                    }
                });
            }
        }

        public static void ClearCache()
        {
            lock (CacheLock)
            {
                Cache.Clear();
            }
        }

        public static void UpdateDiscoveredModels(string modelsDirectory, List<ModelBundleInfo> bundles,
            HashSet<string> changedBundles, CancellationToken cancellationToken = default)
        {
            var root = Path.GetFullPath(modelsDirectory);
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var previousByName = new Dictionary<string, ModelBundleInfo>(StringComparer.Ordinal);
            foreach (var bundle in bundles)
                if (IsYsm(bundle) && !previousByName.ContainsKey(bundle.BundleName))
                    previousByName.Add(bundle.BundleName, bundle);
            Scan(root, 0);
            foreach (var path in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                var bundleName = "YSM/" + relative;
                previousByName.TryGetValue(bundleName, out var previous);
                try
                {
                    var profile = ReadProfile(path);
                    var bundlePath = ResolveProfileBundlePath(root, path, profile);
                    var sourceBundle = ModelBundleInfo.CreateSourceBundle(root, bundleName, "Ysm", []);
                    sourceBundle.BundlePath = bundlePath;
                    var assetPath = AssetBundleManager.FindYsmAssetPath(sourceBundle, relative);
                    var revision = Fingerprint(path, cancellationToken)
                                   + (assetPath == null ? string.Empty : "|" + BundleRevision(sourceBundle, assetPath, path));
                    if (previous?.Models.FirstOrDefault()?.SourceRevision == revision) continue;
                    var package = LoadValidated(path, profile,
                        assetPath == null ? null : AssetBundleManager.LoadYsmBytes(sourceBundle, assetPath));
                    if (Fingerprint(path, cancellationToken)
                        + (assetPath == null ? string.Empty : "|" + BundleRevision(sourceBundle, assetPath, path)) != revision)
                        throw new IOException("YSM source changed while discovering.");
                    var thumbnailPath = FindThumbnail(path, profile);
                    var model = new ModelInfo
                    {
                        ModelID = string.IsNullOrWhiteSpace(profile.ModelID) ? "ysm:" + relative : profile.ModelID,
                        Name = FirstText(profile.Name, (string?)package.Manifest["metadata"]?["name"],
                            Path.GetFileNameWithoutExtension(path)),
                        Author = FirstText(profile.Author, ReadAuthors(package), string.Empty),
                        Description = FirstText(profile.Description, (string?)package.Manifest["metadata"]?["tips"],
                            string.Empty),
                        ThumbnailPath = thumbnailPath,
                        ThumbnailData = thumbnailPath.Length == 0 &&
                                        package.Resources.TryGetValue("thumbnail/thumb-button", out var thumbnail)
                            ? thumbnail
                            : null,
                        SourceKind = "Ysm", SourcePath = relative, SourceRevision = revision,
                        DeathLootBoxYsmPath = ResolveDeathLootBoxPath(root, path, profile.DeathLootBoxYsmPath,
                            bundlePath),
                        DeathLootBoxPrefabPath = profile.DeathLootBoxPrefabPath,
                        DeathLootBoxAnimation = profile.DeathLootBoxAnimation,
                        BundleName = bundleName, TargetTypes = profile.TargetTypes.Length == 0
                            ? [ModelTargetType.Character, ModelTargetType.AllAICharacters]
                            : profile.TargetTypes,
                        Features = [ModelFeatures.NoAutoShaderReplace, ModelFeatures.SkipShowBackMaterial],
                    };
                    var document = package.Models[profile.ModelTarget];
                    model.Version = document.Version;
                    if (!model.Validate()) throw new InvalidDataException("Invalid YSM model metadata.");
                    lock (CacheLock)
                    {
                        var cacheKey = path + "|" + revision;
                        if (!Cache.TryGetValue(cacheKey, out var cached))
                            Cache.Add(cacheKey, new() { Package = package, LastUsed = ++cacheUseSequence });
                        else cached.LastUsed = ++cacheUseSequence;
                        TrimIdlePackages();
                    }

                    var bundle = ModelBundleInfo.CreateSourceBundle(root, bundleName, "Ysm", [model]);
                    bundle.BundlePath = bundlePath;
                    if (previous != null) bundles.Remove(previous);
                    bundles.Add(bundle);
                    changedBundles.Add(bundleName);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ModLogger.LogWarning(
                        $"YSM source '{relative}' could not be refreshed; keeping the previous registration: {ex.Message}");
                }
            }

            foreach (var bundle in bundles.Where(IsYsm).ToArray())
            {
                var model = bundle.Models.FirstOrDefault();
                if (model == null) continue;
                var path = Path.GetFullPath(Path.Combine(root, model.SourcePath));
                var parent = Path.GetDirectoryName(path);
                if (File.Exists(path) || Directory.Exists(path) || parent == null ||
                    !visited.Contains(parent)) continue;
                bundles.Remove(bundle);
                changedBundles.Add(bundle.BundleName);
            }

            void Scan(string directory, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    RejectLinks(directory);
                    if (depth > 8) return;
                    if (directory != root && File.Exists(Path.Combine(directory, "bundleinfo.json"))) return;
                    if (directory != root && (File.Exists(Path.Combine(directory, "ysm.json"))
                                              || File.Exists(Path.Combine(directory, "main.json"))))
                    {
                        candidates.Add(directory);
                        return;
                    }

                    visited.Add(directory);
                    foreach (var file in Directory.EnumerateFiles(directory))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Path.GetExtension(file).Equals(".ysm", StringComparison.OrdinalIgnoreCase))
                            candidates.Add(file);
                    }

                    foreach (var child in Directory.EnumerateDirectories(directory)) Scan(child, depth + 1);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ModLogger.LogWarning($"YSM discovery skipped an unreadable directory: {ex.Message}");
                }
            }
        }

        private static ModelPackage LoadValidated(string path, DuckovYsmBindingProfile profile, byte[]? bundleBytes = null)
        {
            if (bundleBytes == null) RejectLinks(path);
            var package = bundleBytes == null
                ? new ModelLoader(new(), new YsmDecoder()).Load(path)
                : new YsmDecoder().Decode(bundleBytes, new LoadLimits());
            if (!package.Models.TryGetValue(profile.ModelTarget, out var document))
                throw new InvalidDataException("YSM package lacks configured target '" + profile.ModelTarget + "'.");
            if (profile.ModelTarget == "player/main")
            {
                OriginalAnimations.Add(document, new(document.Animations.Keys, StringComparer.Ordinal));
                YsmAnimationFallbacks.Apply(document);
            }
            document.Validate();
            return package;
        }

        private static void TrimIdlePackages()
        {
            while (true)
            {
                string? oldestKey = null;
                var oldestUse = long.MaxValue;
                var idleCount = 0;
                foreach (var pair in Cache)
                {
                    if (pair.Value.References != 0) continue;
                    idleCount++;
                    if (pair.Value.LastUsed >= oldestUse) continue;
                    oldestUse = pair.Value.LastUsed;
                    oldestKey = pair.Key;
                }

                if (idleCount <= MaxIdlePackages || oldestKey == null) return;
                Cache.Remove(oldestKey);
            }
        }

        private static DuckovYsmBindingProfile ReadProfile(string sourcePath)
        {
            var path = ProfilePath(sourcePath);
            var profile = new DuckovYsmBindingProfile();
            if (File.Exists(path))
            {
                RejectLinks(path);
                using var input = File.OpenRead(path);
                var bytes = MemoryAssetSource.ReadBounded(input, 1024 * 1024);
                profile = JsonConvert.DeserializeObject<DuckovYsmBindingProfile>(Encoding.UTF8.GetString(bytes),
                              ProfileSettings)
                          ?? throw new InvalidDataException("YSM binding profile must be a JSON object.");
            }

            profile.Validate();
            return profile;
        }

        private static string ProfilePath(string path)
        {
            return Directory.Exists(path)
                ? Path.Combine(path, Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)) + ".ysm.duckov.json")
                : path + ".duckov.json";
        }

        private static string ResolveDeathLootBoxPath(string root, string sourcePath, string relative,
            string bundlePath)
        {
            if (string.IsNullOrWhiteSpace(relative)) return string.Empty;
            if (Path.IsPathRooted(relative) || !relative.EndsWith(".ysm", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("DeathLootBoxYsmPath must name a relative .ysm file.");
            var sourceDirectory = Directory.Exists(sourcePath) ? sourcePath : Path.GetDirectoryName(sourcePath)!;
            var path = Path.GetFullPath(Path.Combine(sourceDirectory, relative));
            var modelsRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (!path.StartsWith(modelsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("DeathLootBoxYsmPath must remain inside Models.");
            var target = Path.GetRelativePath(modelsRoot, path).Replace('\\', '/');
            if (bundlePath.Length != 0)
            {
                var bundle = ModelBundleInfo.CreateSourceBundle(modelsRoot, string.Empty, "Ysm", []);
                bundle.BundlePath = bundlePath;
                if (AssetBundleManager.FindYsmAssetPath(bundle, target) != null) return target;
            }
            if (!File.Exists(path))
                throw new InvalidDataException("DeathLootBoxYsmPath must resolve to a YSM file inside Models.");
            RejectLinks(path);
            return target;
        }

        private static string BundleRevision(ModelBundleInfo bundle, string assetPath, string sourcePath)
        {
            var path = Path.GetFullPath(Path.Combine(bundle.DirectoryPath, bundle.BundlePath));
            var info = new FileInfo(path);
            var profilePath = ProfilePath(sourcePath);
            var profile = File.Exists(profilePath) ? new FileInfo(profilePath) : null;
            return path + "|" + assetPath + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks
                   + "|" + profile?.Length + "|" + profile?.LastWriteTimeUtc.Ticks;
        }

        private static string ResolveProfileBundlePath(string root, string sourcePath,
            DuckovYsmBindingProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.BundlePath))
            {
                if (!string.IsNullOrWhiteSpace(profile.DeathLootBoxPrefabPath))
                    throw new InvalidDataException("DeathLootBoxPrefabPath requires BundlePath.");
                return string.Empty;
            }

            if (Path.IsPathRooted(profile.BundlePath))
                throw new InvalidDataException("BundlePath must be relative to the YSM source directory.");
            var sourceDirectory = Directory.Exists(sourcePath) ? sourcePath : Path.GetDirectoryName(sourcePath)!;
            var absolute = Path.GetFullPath(Path.Combine(sourceDirectory, profile.BundlePath));
            var relative = Path.GetRelativePath(root, absolute).Replace('\\', '/');
            ResolveInside(root, relative);
            if (!File.Exists(Path.Combine(root, relative)))
                throw new FileNotFoundException("YSM sidecar AssetBundle was not found.", relative);
            return relative;
        }

        private static string FirstText(string first, string? second, string fallback)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : !string.IsNullOrWhiteSpace(second) ? second : fallback;
        }

        private static string ReadAuthors(ModelPackage package)
        {
            var authors = package.Manifest["metadata"]?["authors"] as JArray;
            return authors == null
                ? string.Empty
                : string.Join(", ", authors
                    .Select(author => author.Type == JTokenType.String
                        ? (string?)author
                        : (string?)author["name"])
                    .Where(name => !string.IsNullOrWhiteSpace(name)));
        }

        private static string FindThumbnail(string sourcePath, DuckovYsmBindingProfile profile)
        {
            var directory = Directory.Exists(sourcePath) ? sourcePath : Path.GetDirectoryName(sourcePath)!;
            if (!string.IsNullOrWhiteSpace(profile.ThumbnailPath))
            {
                var configured = ResolveInside(directory, profile.ThumbnailPath);
                if (!File.Exists(configured))
                    throw new FileNotFoundException("YSM thumbnail was not found.", configured);
                return configured;
            }

            var candidates = Directory.Exists(sourcePath)
                ? new[] { Path.Combine(sourcePath, "thumbnail.png"), Path.Combine(sourcePath, "preview.png") }
                : new[] { sourcePath + ".png", Path.ChangeExtension(sourcePath, ".png") };
            foreach (var candidate in candidates)
                if (File.Exists(candidate))
                {
                    RejectLinks(candidate);
                    return candidate;
                }

            return string.Empty;
        }

        private static string ResolveInside(string directory, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                throw new InvalidDataException("YSM source must use a relative path inside Models.");
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("YSM source escapes its model directory.");
            RejectLinks(path);
            return path;
        }

        private static void RejectLinks(string path)
        {
            for (var current = Path.GetFullPath(path);
                 !string.IsNullOrEmpty(current);
                 current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current))
                    && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("YSM sources may not traverse symbolic links or junctions.");
        }

        private static string Fingerprint(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectLinks(path);
            var limits = new LoadLimits();
            long total = 0;
            var files = new List<string>();
            if (Directory.Exists(path)) Walk(path, 0);
            else files.Add(path);
            var profilePath = ProfilePath(path);
            if (File.Exists(profilePath) && !files.Contains(profilePath)) files.Add(profilePath);
            var thumbnailPath = FindThumbnail(path, ReadProfile(path));
            if (thumbnailPath.Length != 0 && !files.Contains(thumbnailPath)) files.Add(thumbnailPath);
            files.Sort(StringComparer.Ordinal);
            using var digest = SHA256.Create();
            var buffer = new byte[81920];
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RejectLinks(file);
                var info = new FileInfo(file);
                if (info.Length > limits.MaxFileBytes || (total += info.Length) > limits.MaxTotalBytes)
                    throw new InvalidDataException("YSM source exceeds the configured loading budget.");
                var name = Encoding.UTF8.GetBytes(file + "\n" + info.Length + "\n");
                digest.TransformBlock(name, 0, name.Length, name, 0);
                using var input = File.OpenRead(file);
                long readTotal = 0;
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if ((readTotal += count) > info.Length) throw new IOException("YSM source changed while hashing.");
                    digest.TransformBlock(buffer, 0, count, buffer, 0);
                }

                if (readTotal != info.Length) throw new IOException("YSM source changed while hashing.");
            }

            digest.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(digest.Hash!).Replace("-", string.Empty);

            void Walk(string directory, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RejectLinks(directory);
                if (depth > 32) throw new InvalidDataException("YSM source exceeds directory depth limit.");
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    if (files.Count >= limits.MaxEntries)
                        throw new InvalidDataException("YSM source has too many files.");
                    files.Add(file);
                }

                foreach (var child in Directory.EnumerateDirectories(directory)) Walk(child, depth + 1);
            }
        }

        private sealed class CacheEntry
        {
            public long LastUsed;
            public ModelPackage Package = null!;
            public int References;
        }
    }
}
