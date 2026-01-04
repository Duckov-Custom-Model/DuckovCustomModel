using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using UnityEngine;

namespace DuckovCustomModel.Core.Managers
{
    public static class ModelTargetTypeRegistry
    {
        private static readonly Dictionary<string, ModelTargetTypeInfo> RegisteredTypes = new();
        private static readonly Dictionary<string, HashSet<string>> CompatibleTypesCache = new();

        public static IReadOnlyDictionary<string, ModelTargetTypeInfo> AllRegisteredTypes => RegisteredTypes;

        public static void RegisterTargetType(string targetTypeId, string[]? compatibleBuiltInTargetTypes = null,
            Func<SystemLanguage, string>? getDisplayName = null)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId))
                throw new ArgumentException("Target type ID cannot be null or empty.", nameof(targetTypeId));

            if (!targetTypeId.StartsWith(ModelTargetType.ExtensionPrefix))
            {
                var prefixedId = ModelTargetType.ExtensionPrefix + targetTypeId;
                targetTypeId = prefixedId;
            }

            var targetTypeInfo = new ModelTargetTypeInfo(targetTypeId, compatibleBuiltInTargetTypes, getDisplayName);
            if (!RegisteredTypes.TryAdd(targetTypeId, targetTypeInfo))
                throw new InvalidOperationException($"Target type '{targetTypeId}' is already registered.");

            CompatibleTypesCache.Remove(targetTypeId);
        }

        public static bool UnregisterTargetType(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return false;

            if (!targetTypeId.StartsWith(ModelTargetType.ExtensionPrefix))
                targetTypeId = ModelTargetType.ExtensionPrefix + targetTypeId;

            var removed = RegisteredTypes.Remove(targetTypeId);
            if (removed) CompatibleTypesCache.Remove(targetTypeId);
            return removed;
        }

        public static bool IsRegistered(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return false;
            return RegisteredTypes.ContainsKey(targetTypeId) || ModelTargetType.IsBuiltIn(targetTypeId);
        }

        public static ModelTargetTypeInfo? GetTargetType(string targetTypeId)
        {
            return string.IsNullOrWhiteSpace(targetTypeId) ? null : RegisteredTypes.GetValueOrDefault(targetTypeId);
        }

        public static HashSet<string> GetCompatibleModelTargetTypes(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return [];

            if (CompatibleTypesCache.TryGetValue(targetTypeId, out var cached))
                return [..cached];

            var compatible = new HashSet<string> { targetTypeId };

            if (ModelTargetType.IsBuiltIn(targetTypeId))
            {
                CompatibleTypesCache[targetTypeId] = compatible;
                return compatible;
            }

            if (RegisteredTypes.TryGetValue(targetTypeId, out var type))
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (type.CompatibleBuiltInTargetTypes != null)
                    foreach (var compatibleBuiltIn in type.CompatibleBuiltInTargetTypes)
                        if (ModelTargetType.IsValid(compatibleBuiltIn))
                            compatible.Add(compatibleBuiltIn);

            CompatibleTypesCache[targetTypeId] = compatible;
            return compatible;
        }

        public static List<string> GetAllAvailableTargetTypes()
        {
            var types = new List<string>
            {
                ModelTargetType.Character,
                ModelTargetType.Pet,
                ModelTargetType.AllAICharacters,
            };

            types.AddRange(RegisteredTypes.Keys);
            return types;
        }
    }
}
