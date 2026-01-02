using System;
using UnityEngine;

namespace DuckovCustomModel.Core.Data
{
    public class ModelTargetTypeInfo
    {
        public ModelTargetTypeInfo(string targetTypeId, string[]? compatibleBuiltInTargetTypes = null,
            Func<SystemLanguage, string>? getDisplayName = null)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId))
                throw new ArgumentException("Target type ID cannot be null or empty.", nameof(targetTypeId));

            TargetTypeId = targetTypeId;
            CompatibleBuiltInTargetTypes = compatibleBuiltInTargetTypes ?? [];
            GetDisplayName = getDisplayName ?? (_ => targetTypeId);
        }

        public string TargetTypeId { get; }
        public string[] CompatibleBuiltInTargetTypes { get; }
        public Func<SystemLanguage, string> GetDisplayName { get; }
    }
}
