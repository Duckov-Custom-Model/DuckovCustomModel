using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomModel.Core;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;

namespace DuckovCustomModel.Configs
{
    public class UsingModel : ConfigBase
    {
        public int Version { get; set; } = 2;

        public Dictionary<string, string> TargetTypeModelIDs { get; set; } = [];

        public string GetModelID(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return string.Empty;
            return TargetTypeModelIDs.TryGetValue(targetTypeId, out var modelID) ? modelID : string.Empty;
        }

        public void SetModelID(string targetTypeId, string modelID)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;

            if (string.IsNullOrEmpty(modelID))
                TargetTypeModelIDs.Remove(targetTypeId);
            else
                TargetTypeModelIDs[targetTypeId] = modelID;
        }

        public override void LoadDefault()
        {
            Version = 2;
            TargetTypeModelIDs.Clear();
#pragma warning disable CS0618
            ModelIDs.Clear();
            AICharacterModelIDs.Clear();
#pragma warning restore CS0618
        }

        public override bool Validate()
        {
            var modified = false;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (TargetTypeModelIDs == null)
            {
                TargetTypeModelIDs = [];
                modified = true;
            }

#pragma warning disable CS0618
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            ModelIDs ??= [];

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            AICharacterModelIDs ??= [];

            var hasOldData = ModelIDs.Count > 0 || AICharacterModelIDs.Count > 0;
            var hasNewData = TargetTypeModelIDs.Count > 0;
            var needsMigration = Version < 2 || (Version >= 2 && hasOldData && !hasNewData);

            if (!needsMigration) return modified;
            MigrateToVersion2();
            Version = 2;
            modified = true;
#pragma warning restore CS0618

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UsingModel otherSetting) return;
            Version = otherSetting.Version;
            TargetTypeModelIDs = new(otherSetting.TargetTypeModelIDs);
#pragma warning disable CS0618
            ModelIDs = new(otherSetting.ModelIDs);
            AICharacterModelIDs = new(otherSetting.AICharacterModelIDs);
#pragma warning restore CS0618
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
            TargetTypeModelIDs ??= [];

            foreach (var (target, modelID) in ModelIDs)
            {
                if (string.IsNullOrEmpty(modelID)) continue;
                var targetTypeId = target.ToTargetTypeId();
                TargetTypeModelIDs[targetTypeId] = modelID;
            }

            foreach (var (nameKey, modelID) in AICharacterModelIDs)
            {
                if (string.IsNullOrEmpty(modelID)) continue;
                if (nameKey == AICharacters.AllAICharactersKey)
                {
                    TargetTypeModelIDs[ModelTargetType.AllAICharacters] = modelID;
                }
                else
                {
                    var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
                    TargetTypeModelIDs[targetTypeId] = modelID;
                }
            }
#pragma warning restore CS0618
        }

        #region 过时成员（向后兼容）

        [Obsolete("Use TargetTypeModelIDs instead. This property is kept for backward compatibility.")]
        public Dictionary<ModelTarget, string> ModelIDs { get; set; } = [];

        [Obsolete("Use TargetTypeModelIDs instead. This property is kept for backward compatibility.")]
        public Dictionary<string, string> AICharacterModelIDs { get; set; } = [];

        [Obsolete("Use GetModelID(string targetTypeId) instead.")]
        public string GetModelID(ModelTarget target)
        {
            var targetTypeId = target.ToTargetTypeId();
            return GetModelID(targetTypeId);
        }

        [Obsolete("Use SetModelID(string targetTypeId, string modelID) instead.")]
        public void SetModelID(ModelTarget target, string modelID)
        {
            var targetTypeId = target.ToTargetTypeId();
            SetModelID(targetTypeId, modelID);
        }

        [Obsolete("Use GetModelID(string targetTypeId) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public string GetAICharacterModelID(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return string.Empty;
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            return GetModelID(targetTypeId);
        }

        [Obsolete("Use GetModelID with fallback logic instead.")]
        public string GetAICharacterModelIDWithFallback(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return string.Empty;

            var specificTargetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            var modelID = GetModelID(specificTargetTypeId);
            if (!string.IsNullOrEmpty(modelID)) return modelID;

            var fallbackModelID = GetModelID(ModelTargetType.AllAICharacters);
            return fallbackModelID;
        }

        [Obsolete(
            "Use SetModelID(string targetTypeId, string modelID) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public void SetAICharacterModelID(string nameKey, string modelID)
        {
            if (string.IsNullOrEmpty(nameKey)) return;
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            SetModelID(targetTypeId, modelID);
        }

        #endregion
    }
}
