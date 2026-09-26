using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Core.Managers;
using Newtonsoft.Json;

namespace DuckovCustomModel.Core.Data
{
    public class ModelInfo : IValidatable
    {
        public string ModelID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        [JsonIgnore] public byte[]? ThumbnailData { get; set; }
        public string PrefabPath { get; set; } = string.Empty;
        public string SourceKind { get; set; } = "AssetBundle";
        public string SourcePath { get; set; } = string.Empty;
        [JsonIgnore] public string SourceRevision { get; set; } = string.Empty;

        public string? DeathLootBoxPrefabPath { get; set; }

        public SoundInfo[] CustomSounds { get; set; } = [];

        public Dictionary<string, float>? SoundTagPlayChance { get; set; }

        [JsonIgnore] public string BundleName { get; set; } = string.Empty;

        public string[] TargetTypes { get; set; } = [];

        public string[] Features { get; set; } = [];

        public float? WalkSoundFrequency { get; set; }

        public float? RunSoundFrequency { get; set; }

        public Dictionary<string, BuffCondition[]>? BuffAnimatorParams { get; set; }

        public RadialMenuInfo[] RadialMenus { get; set; } = [];

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(ModelID)) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (string.Equals(SourceKind, "Ysm", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(SourcePath)) return false;
            }
            else if (!string.Equals(SourceKind, "AssetBundle", StringComparison.OrdinalIgnoreCase)
                     || string.IsNullOrWhiteSpace(PrefabPath))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(DeathLootBoxPrefabPath)) DeathLootBoxPrefabPath = null;

            var targetTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (TargetTypes != null)
                foreach (var targetType in TargetTypes)
                {
                    if (string.IsNullOrWhiteSpace(targetType)) continue;
                    if (!ModelTargetType.IsValid(targetType)) continue;
                    targetTypes.Add(targetType);
                }

#pragma warning disable CS0618
            var hasLegacyData = Target is { Length: > 0 } || SupportedAICharacters is { Length: > 0 };
#pragma warning restore CS0618
            if (hasLegacyData)
                MigrateFromLegacyProperties(targetTypes);

            if (targetTypes.Count == 0)
                targetTypes.Add(ModelTargetType.Character);

            TargetTypes = targetTypes.ToArray();

            var soundInfos = new List<SoundInfo>();
            foreach (var soundInfo in CustomSounds ?? [])
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (soundInfo == null || string.IsNullOrWhiteSpace(soundInfo.Path)) continue;
                soundInfo.Initialize();
                soundInfos.Add(soundInfo);
            }

            var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var feature in Features ?? [])
            {
                if (string.IsNullOrWhiteSpace(feature)) continue;
                features.Add(feature.Trim());
            }

            Features = features.ToArray();
            CustomSounds = soundInfos.ToArray();

            if (string.Equals(SourceKind, "AssetBundle", StringComparison.OrdinalIgnoreCase))
            {
                var actionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var actionParameters = new HashSet<string>(StringComparer.Ordinal);
                RadialMenus = ValidateRadialMenus(RadialMenus, actionIds, actionParameters,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            }
            else
            {
                RadialMenus = [];
            }

            return true;
        }

        private static RadialActionInfo[] ValidateRadialActions(RadialActionInfo[]? actions,
            HashSet<string> actionIds, HashSet<string> actionParameters)
        {
            return (actions ?? [])
                .Where(action => action != null && action.Validate() && actionIds.Add(action.Id)
                                 && actionParameters.Add(action.Parameter))
                .ToArray();
        }

        private static RadialMenuInfo[] ValidateRadialMenus(RadialMenuInfo[]? menus,
            HashSet<string> actionIds, HashSet<string> actionParameters, HashSet<string> menuIds, int depth)
        {
            if (depth >= 5) return [];
            var valid = new List<RadialMenuInfo>();
            foreach (var menu in menus ?? [])
            {
                if (menu == null || !menu.Validate() || !menuIds.Add(menu.Id)) continue;
                menu.Actions = ValidateRadialActions(menu.Actions, actionIds, actionParameters);
                menu.Menus = ValidateRadialMenus(menu.Menus, actionIds, actionParameters, menuIds, depth + 1);
                if (menu.Actions.Length > 0 || menu.Menus.Length > 0) valid.Add(menu);
            }

            return valid.ToArray();
        }

        public bool CompatibleWithTargetType(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return false;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (TargetTypes == null || TargetTypes.Length == 0) return false;

            if (Array.Exists(TargetTypes, t => t.Equals(targetTypeId, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (ModelTargetType.IsAICharacterTargetType(targetTypeId))
            {
                var nameKey = ModelTargetType.ExtractAICharacterName(targetTypeId);
                if (!string.IsNullOrEmpty(nameKey) && nameKey != AICharacters.AllAICharactersKey)
                    if (Array.Exists(TargetTypes,
                            t => t.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase)))
                        return true;
            }

            if (!ModelTargetType.IsExtension(targetTypeId)) return false;

            var compatibleTypes = ModelTargetTypeRegistry.GetCompatibleModelTargetTypes(targetTypeId);
            return compatibleTypes.Any(compatibleType =>
                Array.Exists(TargetTypes, t => t.Equals(compatibleType, StringComparison.OrdinalIgnoreCase)));
        }

        public bool CompatibleWithAICharacter(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return false;
            if (nameKey == AICharacters.AllAICharactersKey) return true;

            var specificTargetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            return CompatibleWithTargetType(specificTargetTypeId) ||
                   CompatibleWithTargetType(ModelTargetType.AllAICharacters);
        }

        private void MigrateFromLegacyProperties(HashSet<string> targetTypes)
        {
#pragma warning disable CS0618
            var hasAICharacterMarker = Target is { Length: > 0 } &&
                                       Array.Exists(Target, t => t == ModelTarget.AICharacter);

            if (Target is { Length: > 0 })
                foreach (var target in Target)
                {
                    if (!Enum.IsDefined(typeof(ModelTarget), target)) continue;
                    if (target == ModelTarget.AICharacter) continue;
                    var targetTypeId = target.ToTargetTypeId();
                    targetTypes.Add(targetTypeId);
                }

            if (!hasAICharacterMarker || SupportedAICharacters is not { Length: > 0 }) return;
            foreach (var nameKey in SupportedAICharacters)
            {
                if (string.IsNullOrWhiteSpace(nameKey)) continue;
                targetTypes.Add(nameKey == AICharacters.AllAICharactersKey
                    ? ModelTargetType.AllAICharacters
                    : ModelTargetType.CreateAICharacterTargetType(nameKey));
            }
#pragma warning restore CS0618
        }

        #region 过时成员（向后兼容）

        [Obsolete("Use TargetTypes instead. This property is kept for backward compatibility.")]
        public ModelTarget[] Target { get; set; } = [];

        [Obsolete(
            "Use TargetTypes instead. This property is kept for backward compatibility and will be automatically converted to TargetTypes in Validate().")]
        public string[] SupportedAICharacters { get; set; } = [];

        [Obsolete("Use CompatibleWithTargetType(string targetTypeId) instead.")]
        public bool CompatibleWithType(ModelTarget modelTarget)
        {
            var targetTypeId = modelTarget.ToTargetTypeId();
            return CompatibleWithTargetType(targetTypeId);
        }

        #endregion
    }
}
