using System;
using System.IO;
using System.Reflection;

namespace DuckovCustomModel
{
    public static class ModLoader
    {
        private static Assembly? _loadedAssembly;

        private static string? _modDirectory;

        // Mono cannot unload individual assemblies. Retain their identity/provenance
        // across disable/enable so byte-loaded assemblies are not loaded a second time.
        private static ModDependencyResolver? _dependencyResolver;

        public static void Initialize()
        {
            Uninitialize();
            _modDirectory = Path.GetDirectoryName(typeof(ModLoader).Assembly.Location);
            if (_modDirectory == null)
            {
                ModLogger.LogError("Failed to get assembly directory.");
                return;
            }

            if (_dependencyResolver == null || !_dependencyResolver.IsDirectory(_modDirectory))
                _dependencyResolver = new(_modDirectory);
            AppDomain.CurrentDomain.AssemblyResolve += _dependencyResolver.Resolve;
            HarmonyLoader.OnReadyToPatch += OnReadyToPatch;
            if (ModBehaviour.Instance != null) ModBehaviour.Instance.OnModDisabled += OnModDisabled;
        }

        public static void Uninitialize()
        {
            OnModDisabled();
        }

        private static void OnReadyToPatch()
        {
            if (_loadedAssembly != null || _modDirectory == null || _dependencyResolver == null) return;
            var path = Path.GetDirectoryName(typeof(ModLoader).Assembly.Location);
            if (path == null)
            {
                ModLogger.LogError("Failed to get assembly directory.");
                return;
            }

            var targetAssemblyFile = Path.Combine(path, Constant.TargetAssemblyName);
            if (!File.Exists(targetAssemblyFile))
            {
                ModLogger.LogError($"Target assembly not found: {targetAssemblyFile}");
                return;
            }

            try
            {
                ModLogger.Log($"Loading Assembly from: {targetAssemblyFile}");

                // Unity Mono can resolve field types without a RequestingAssembly. Load our
                // exact local dependencies first so those metadata lookups need no callback.
                foreach (var dependency in new[]
                         {
                             "System.Runtime.CompilerServices.Unsafe.dll",
                             "StbImageSharp.dll", "ZstdSharp.dll",
                             "ModelRuntime.dll", "ModelRuntime.Media.dll",
                             "ModelRuntime.Ysm.dll", "ModelRuntime.Adapters.dll",
                             "DuckovCustomModel.Core.dll",
                         })
                    _dependencyResolver.LoadModule(dependency);

                _loadedAssembly = _dependencyResolver.LoadModule(Constant.TargetAssemblyName);

                ModLogger.Log("Invoking ModEntry.Initialize...");

                InvokeModEntryMethodInitialize();

                if (ModBehaviour.Instance != null)
                {
                    ModBehaviour.Instance.OnModDisabled -= OnModDisabled;
                    ModBehaviour.Instance.OnModDisabled += OnModDisabled;
                }

                ModLogger.Log("ModLoader initialization complete.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error loading target assembly or applying patches: {ex}");
            }
        }

        private static void OnModDisabled()
        {
            HarmonyLoader.OnReadyToPatch -= OnReadyToPatch;
            if (ModBehaviour.Instance != null) ModBehaviour.Instance.OnModDisabled -= OnModDisabled;
            try
            {
                if (_loadedAssembly == null) return;
                ModLogger.Log("Uninitializing Mod...");
                InvokeModEntryMethodUninitialize();
                ModLogger.Log("Mod uninitialization complete.");
            }
            finally
            {
                if (_dependencyResolver != null)
                    AppDomain.CurrentDomain.AssemblyResolve -= _dependencyResolver.Resolve;
                _loadedAssembly = null;
                _modDirectory = null;
            }
        }

        private static void InvokeModEntryMethodInitialize()
        {
            InvokeModEntryMethod("Initialize", [_modDirectory]);
        }

        private static void InvokeModEntryMethodUninitialize()
        {
            InvokeModEntryMethod("Uninitialize");
        }

        private static void InvokeModEntryMethod(string methodName, object?[]? parameters = null)
        {
            if (_loadedAssembly == null)
            {
                ModLogger.LogError("Target assembly is not loaded. Cannot invoke ModEntry methods.");
                return;
            }

            var modEntryType = _loadedAssembly.GetType($"{Constant.ModId}.ModEntry");
            if (modEntryType == null)
            {
                ModLogger.LogError("ModEntry type not found in target assembly.");
                return;
            }

            MethodInfo? method;
            if (parameters != null)
            {
                var parameterTypes = new Type[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                    parameterTypes[i] = parameters[i]?.GetType() ?? typeof(object);
                method = modEntryType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null,
                    parameterTypes, null);
            }
            else
            {
                method = modEntryType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            }

            if (method == null)
            {
                ModLogger.LogError($"ModEntry.{methodName} method not found.");
                return;
            }

            try
            {
                method.Invoke(null, parameters);
                ModLogger.Log($"ModEntry.{methodName} invoked successfully.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error invoking ModEntry.{methodName}: {ex}");
            }
        }
    }
}
