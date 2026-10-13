using System;
using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public sealed class ModelSourceDefinition
    {
        private static readonly Dictionary<string, ModelSourceDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AssetBundle"] = new("AssetBundle", true),
                ["Ysm"] = new("Ysm", false),
            };

        public ModelSourceDefinition(string kind, bool usesPrefabPath)
        {
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Source kind is required.", nameof(kind));
            Kind = kind.Trim();
            UsesPrefabPath = usesPrefabPath;
        }

        public string Kind { get; }
        public bool UsesPrefabPath { get; }

        public static void Register(ModelSourceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            lock (Definitions)
            {
                if (Definitions.TryGetValue(definition.Kind, out var current))
                {
                    if (current.UsesPrefabPath != definition.UsesPrefabPath)
                        throw new InvalidOperationException("Conflicting model source definition: " + definition.Kind);
                    return;
                }

                Definitions.Add(definition.Kind, definition);
            }
        }

        public static bool TryGet(string? kind, out ModelSourceDefinition definition)
        {
            lock (Definitions)
            {
                return Definitions.TryGetValue(kind?.Trim() ?? string.Empty, out definition!);
            }
        }
    }
}
