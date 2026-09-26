using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DuckovCustomModel
{
    internal sealed class ModDependencyResolver
    {
        private static readonly HashSet<string> AllowedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "DuckovCustomModel.Core", "DuckovCustomModel.GameModules", "0Harmony",
            "ModelRuntime", "ModelRuntime.Adapters", "ModelRuntime.Ysm", "ModelRuntime.Media",
            "StbImageSharp", "ZstdSharp", "System.Runtime.CompilerServices.Unsafe",
        };

        private readonly Dictionary<string, Assembly> _assemblies = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Assembly, string> _assemblyPaths = new();
        private readonly string _directory;
        private readonly HashSet<string> _loading = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Assembly> _ownedAssemblies = new();

        private readonly object _sync = new();

        internal ModDependencyResolver(string directory)
        {
            _directory = Path.GetFullPath(directory);
            _ownedAssemblies.Add(typeof(ModLoader).Assembly);
        }

        internal bool IsDirectory(string directory)
        {
            return string.Equals(_directory, Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase);
        }

        internal Assembly? Resolve(object? sender, ResolveEventArgs args)
        {
            lock (_sync)
            {
                if (args.RequestingAssembly == null || !_ownedAssemblies.Contains(args.RequestingAssembly))
                    return null;

                try
                {
                    var requested = new AssemblyName(args.Name);
                    if (requested.Name == null || !AllowedNames.Contains(requested.Name)) return null;
                    return Load(requested);
                }
                catch (Exception ex)
                {
                    ModLogger.LogError($"Failed to resolve mod dependency {args.Name}: {ex}");
                    return null;
                }
            }
        }

        internal Assembly LoadModule(string fileName)
        {
            lock (_sync)
            {
                if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
                    throw new ArgumentException("Expected an assembly file name.", nameof(fileName));
                var path = Path.Combine(_directory, fileName);
                var identity = AssemblyName.GetAssemblyName(path);
                if (identity.Name == null || !AllowedNames.Contains(identity.Name))
                    throw new FileLoadException("Assembly is not in the mod dependency allowlist.", fileName);
                return Load(identity, path) ?? throw new FileLoadException("Unable to load mod assembly.", fileName);
            }
        }

        private Assembly? Load(AssemblyName requested, string? modulePath = null)
        {
            if (_assemblies.TryGetValue(requested.FullName, out var cached))
            {
                VerifyModuleOrigin(cached, modulePath);
                return cached;
            }

            if (requested.Version != null)
            {
                var loaded = FindLoaded(requested);
                if (loaded != null)
                {
                    VerifyModuleOrigin(loaded, modulePath);
                    return RememberExisting(loaded, modulePath != null);
                }
            }

            var path = modulePath ?? Path.Combine(_directory, requested.Name + ".dll");
            if (!File.Exists(path)) return FindLoaded(requested);

            var identity = AssemblyName.GetAssemblyName(path);
            if (!Matches(requested, identity))
            {
                ModLogger.LogError(
                    $"Mod dependency identity mismatch: requested {requested.FullName}, found {identity.FullName}.");
                return null;
            }

            var key = identity.FullName;
            if (_assemblies.TryGetValue(key, out cached))
            {
                VerifyModuleOrigin(cached, modulePath);
                return cached;
            }

            var existing = FindLoaded(identity);
            if (existing != null)
            {
                VerifyModuleOrigin(existing, modulePath);
                return RememberExisting(existing, modulePath != null);
            }

            if (!_loading.Add(key)) return null;
            try
            {
                ModLogger.Log($"Loading mod assembly: {identity.FullName}");
                var assembly = Assembly.Load(File.ReadAllBytes(path));
                _assemblies[key] = assembly;
                _assemblyPaths[assembly] = path;
                _ownedAssemblies.Add(assembly);
                return assembly;
            }
            finally
            {
                _loading.Remove(key);
            }
        }

        private void VerifyModuleOrigin(Assembly assembly, string? modulePath)
        {
            if (modulePath == null) return;
            if (!_assemblyPaths.TryGetValue(assembly, out var loadedPath))
                loadedPath = assembly.IsDynamic ? "" : assembly.Location;
            if (string.IsNullOrEmpty(loadedPath) ||
                !string.Equals(Path.GetFullPath(loadedPath), modulePath, StringComparison.OrdinalIgnoreCase))
                throw new FileLoadException(
                    "A different or unverified game module is already loaded; restart the game before replacing it.",
                    modulePath);
        }

        private Assembly RememberExisting(Assembly assembly, bool verifiedModule = false)
        {
            _assemblies[assembly.GetName().FullName] = assembly;
            if (verifiedModule || IsLocalAssembly(assembly)) _ownedAssemblies.Add(assembly);
            return assembly;
        }

        private bool IsLocalAssembly(Assembly assembly)
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) return false;
            return string.Equals(Path.GetDirectoryName(Path.GetFullPath(assembly.Location)), _directory,
                StringComparison.OrdinalIgnoreCase);
        }

        private static Assembly? FindLoaded(AssemblyName identity)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (Matches(identity, assembly.GetName()))
                    return assembly;
            return null;
        }

        private static bool Matches(AssemblyName requested, AssemblyName actual)
        {
            if (!string.Equals(requested.Name, actual.Name, StringComparison.OrdinalIgnoreCase) ||
                (requested.Version != null && !requested.Version.Equals(actual.Version)) ||
                !string.Equals(requested.CultureName ?? "", actual.CultureName ?? "",
                    StringComparison.OrdinalIgnoreCase))
                return false;

            var requestedToken = requested.GetPublicKeyToken() ?? Array.Empty<byte>();
            var actualToken = actual.GetPublicKeyToken() ?? Array.Empty<byte>();
            if (requestedToken.Length != actualToken.Length) return false;
            for (var i = 0; i < requestedToken.Length; i++)
                if (requestedToken[i] != actualToken[i])
                    return false;
            return requested.ContentType == actual.ContentType;
        }
    }
}
