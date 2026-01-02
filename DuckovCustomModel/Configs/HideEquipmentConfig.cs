using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomModel.Core;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;

namespace DuckovCustomModel.Configs
{
    public class HideEquipmentConfig : ConfigBase
    {
        public int Version { get; set; } = 2;

        public Dictionary<string, bool> TargetTypeHideEquipment { get; set; } = [];

        public override void LoadDefault()
        {
            Version = 2;
            TargetTypeHideEquipment = [];

            var builtInTargetTypes = new[]
            {
                ModelTargetType.Character,
                ModelTargetType.Pet,
            };

            foreach (var targetTypeId in builtInTargetTypes)
                if (TargetTypeHideEquipment.TryAdd(targetTypeId, false))
                {
                }
        }

        public override bool Validate()
        {
            var modified = false;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (TargetTypeHideEquipment == null)
            {
                TargetTypeHideEquipment = [];
                modified = true;
            }

#pragma warning disable CS0618
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            HideEquipment ??= [];

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            HideAICharacterEquipment ??= [];

            var hasOldData = HideEquipment.Count > 0 || HideAICharacterEquipment.Count > 0;
            var hasNewData = TargetTypeHideEquipment.Count > 0;
            var needsMigration = Version < 2 || (Version >= 2 && hasOldData && !hasNewData);

            if (needsMigration)
            {
                MigrateToVersion2();
                Version = 2;
                modified = true;
            }
#pragma warning restore CS0618

            var builtInTargetTypes = new[]
            {
                ModelTargetType.Character,
                ModelTargetType.Pet,
            };

            foreach (var targetTypeId in builtInTargetTypes)
                if (TargetTypeHideEquipment.TryAdd(targetTypeId, false))
                    modified = true;

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not HideEquipmentConfig otherConfig) return;
            Version = otherConfig.Version;
            TargetTypeHideEquipment = new(otherConfig.TargetTypeHideEquipment);
        }

        public override void LoadFromFile(string filePath, bool autoSaveOnLoad = true)
        {
            try
            {
                ConfigManager.CreateDirectoryIfNotExists();

                if (!File.Exists(filePath))
                {
                    ModLogger.LogWarning($"Config file '{filePath}' does not exist. Loading default config.");
                    LoadDefault();
                    if (autoSaveOnLoad) SaveToFile(filePath);
                    return;
                }

                var json = File.ReadAllText(filePath);
                JsonConvert.PopulateObject(json, this, JsonSettings.Default);

                if (Validate() && autoSaveOnLoad) SaveToFile(filePath);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load config from file '{filePath}': {ex.Message}");
                LoadDefault();
                if (autoSaveOnLoad) SaveToFile(filePath);
            }
        }

        private void MigrateToVersion2()
        {
#pragma warning disable CS0618
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            TargetTypeHideEquipment ??= [];

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (HideEquipment != null)
                foreach (var (target, value) in HideEquipment)
                {
                    var targetTypeId = target.ToTargetTypeId();
                    TargetTypeHideEquipment[targetTypeId] = value;
                }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (HideAICharacterEquipment == null) return;
            {
                foreach (var (nameKey, value) in HideAICharacterEquipment)
                    if (nameKey == AICharacters.AllAICharactersKey)
                    {
                        TargetTypeHideEquipment[ModelTargetType.AllAICharacters] = value;
                    }
                    else
                    {
                        var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
                        TargetTypeHideEquipment[targetTypeId] = value;
                    }
            }
#pragma warning restore CS0618
        }

        public bool GetHideEquipment(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return false;
            return TargetTypeHideEquipment.TryGetValue(targetTypeId, out var value) && value;
        }

        public void SetHideEquipment(string targetTypeId, bool value)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;
            TargetTypeHideEquipment[targetTypeId] = value;
        }

        #region 过时成员（向后兼容）

        [Obsolete("Use TargetTypeHideEquipment instead. This property is kept for backward compatibility.")]
        public Dictionary<ModelTarget, bool> HideEquipment { get; set; } = [];

        [Obsolete("Use TargetTypeHideEquipment instead. This property is kept for backward compatibility.")]
        public Dictionary<string, bool> HideAICharacterEquipment { get; set; } = [];

        [Obsolete("Use GetHideEquipment(string targetTypeId) instead.")]
        public bool GetHideEquipment(ModelTarget target)
        {
            var targetTypeId = target.ToTargetTypeId();
            return GetHideEquipment(targetTypeId);
        }

        [Obsolete("Use SetHideEquipment(string targetTypeId, bool value) instead.")]
        public void SetHideEquipment(ModelTarget target, bool value)
        {
            var targetTypeId = target.ToTargetTypeId();
            SetHideEquipment(targetTypeId, value);
        }

        [Obsolete(
            "Use GetHideEquipment(string targetTypeId) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public bool GetHideAICharacterEquipment(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return GetHideEquipment(ModelTargetType.AllAICharacters);
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            return TargetTypeHideEquipment.TryGetValue(targetTypeId, out var value)
                ? value
                : GetHideEquipment(ModelTargetType.AllAICharacters);
        }

        [Obsolete(
            "Use SetHideEquipment(string targetTypeId, bool value) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public void SetHideAICharacterEquipment(string nameKey, bool value)
        {
            if (string.IsNullOrEmpty(nameKey))
            {
                SetHideEquipment(ModelTargetType.AllAICharacters, value);
                return;
            }

            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            SetHideEquipment(targetTypeId, value);
        }

        #endregion
    }
}
