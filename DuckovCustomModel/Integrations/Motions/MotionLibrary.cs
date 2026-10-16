using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DuckovCustomModel.Core.Animations;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;

namespace DuckovCustomModel.Integrations.Motions
{
    internal static class MotionLibrary
    {
        private static readonly object Sync = new();
        private static readonly SemaphoreSlim Workers = new(2);

        private static readonly Dictionary<string, (long ticks, long length, Task<VmdMotion> task)> Cache =
            new(StringComparer.Ordinal);

        private static string _revision = string.Empty;
        private static RadialActionInfo[] _actions = [];

        internal static string DirectoryPath =>
            Path.Combine(Path.GetDirectoryName(ModelManager.ModelsDirectory)!, "Motions");

        internal static IReadOnlyList<RadialActionInfo> Actions => _actions;

        internal static void Refresh(bool clearCache)
        {
            if (clearCache)
            {
                ClearCache();
            }

            var actions = new List<RadialActionInfo>();
            var revision = new StringBuilder();
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                Scan(DirectoryPath, 0);
            }
            catch (Exception exception)
            {
                ModLogger.LogWarning($"Motion library: {exception.Message}");
            }

            var signature = revision.ToString();
            if (clearCache || signature != _revision)
            {
                _revision = signature;
                _actions = actions.OrderBy(action => action.Name, StringComparer.Ordinal).ToArray();
            }

            void Scan(string directory, int depth)
            {
                if (depth > 5 || actions.Count >= 1024) return;
                ModelSourcePaths.RejectLinks(directory);
                foreach (var file in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
                {
                    var extension = Path.GetExtension(file);
                    if (!extension.Equals(".vmd", StringComparison.OrdinalIgnoreCase) &&
                        !extension.Equals(".vrma", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        ModelSourcePaths.RejectLinks(file);
                        var action = new RadialActionInfo();
                        var settings = Path.ChangeExtension(file, ".motion.json");
                        if (File.Exists(settings))
                        {
                            ModelSourcePaths.RejectLinks(settings);
                            if (new FileInfo(settings).Length > 65536)
                                throw new InvalidDataException("Motion settings are too large.");
                            action = JsonConvert.DeserializeObject<RadialActionInfo>(File.ReadAllText(settings)) ??
                                     action;
                        }

                        var relative = Path.GetRelativePath(DirectoryPath, file).Replace('\\', '/');
                        using var digest = SHA256.Create();
                        action.Id = "dcm:motion/" + BitConverter.ToString(digest.ComputeHash(
                                Encoding.UTF8.GetBytes(relative.Normalize(NormalizationForm.FormC))))
                            .Replace("-", string.Empty).ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(action.Name))
                            action.Name = Path.GetFileNameWithoutExtension(file);
                        action.Motion = Path.GetRelativePath(DirectoryPath, file).Replace('\\', '/');
                        action.Parameter = string.Empty;
                        if (!action.Validate()) throw new InvalidDataException("Invalid motion settings.");
                        var info = new FileInfo(file);
                        revision.Append(relative).Append(info.Length).Append(info.LastWriteTimeUtc.Ticks)
                            .Append(JsonConvert.SerializeObject(action)).AppendLine();
                        actions.Add(action);
                        if (actions.Count >= 1024) break;
                    }
                    catch (Exception exception)
                    {
                        ModLogger.LogWarning($"Motion '{file}': {exception.Message}");
                    }
                }

                foreach (var child in Directory.EnumerateDirectories(directory)
                             .OrderBy(path => path, StringComparer.Ordinal))
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                        Scan(child, depth + 1);
            }
        }

        internal static void ClearCache()
        {
            lock (Sync) Cache.Clear();
            VrmaPose.ClearCache();
        }

        internal static string Resolve(string directory, string relative)
        {
            relative = relative.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                throw new InvalidDataException("Motion resources require a relative path.");
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(root, relative));
            var comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!path.StartsWith(root, comparison))
                throw new InvalidDataException("Motion path escapes its directory.");
            ModelSourcePaths.RejectLinks(path);
            return path;
        }

        internal static Task<VmdMotion> LoadVmd(string path, byte[]? embedded = null)
        {
            if (embedded != null) return ParseAsync(embedded);
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > 64 * 1024 * 1024)
                throw new InvalidDataException("Motion file is missing or too large.");
            lock (Sync)
            {
                if (Cache.TryGetValue(path, out var cached) && cached.ticks == info.LastWriteTimeUtc.Ticks &&
                    cached.length == info.Length && !cached.task.IsFaulted) return cached.task;
                if (Cache.Count >= 16 || Cache.Values.Sum(value => value.length) + info.Length > 64 * 1024 * 1024)
                    Cache.Clear();
                var task = Task.Run(async () =>
                {
                    await Workers.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        return VmdMotion.Parse(File.ReadAllBytes(path));
                    }
                    finally
                    {
                        Workers.Release();
                    }
                });
                Cache[path] = (info.LastWriteTimeUtc.Ticks, info.Length, task);
                return task;
            }
        }

        private static Task<VmdMotion> ParseAsync(byte[] bytes) => Task.Run(async () =>
        {
            await Workers.WaitAsync().ConfigureAwait(false);
            try
            {
                return VmdMotion.Parse(bytes);
            }
            finally
            {
                Workers.Release();
            }
        });
    }
}
