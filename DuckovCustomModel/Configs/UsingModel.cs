using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomModel.Data;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DuckovCustomModel.Configs
{
    public class UsingModel : ConfigBase
    {
        public Dictionary<ModelTarget, string> ModelIDs { get; set; } = [];

        public string GetModelID(ModelTarget target)
        {
            return ModelIDs.TryGetValue(target, out var modelID) ? modelID : string.Empty;
        }

        public void SetModelID(ModelTarget target, string modelID)
        {
            if (string.IsNullOrEmpty(modelID))
                ModelIDs.Remove(target);
            else
                ModelIDs[target] = modelID;
        }

        public override void LoadDefault()
        {
            ModelIDs.Clear();
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UsingModel otherSetting) return;
            ModelIDs = new(otherSetting.ModelIDs);
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
                var jsonObject = JObject.Parse(json);

                var hasOldFormat = jsonObject["ModelID"] != null || jsonObject["PetModelID"] != null;
                var hasNewFormat = jsonObject["ModelIDs"] != null;

                if (hasOldFormat && !hasNewFormat)
                {
                    ModelIDs.Clear();
                    var modelID = jsonObject["ModelID"]?.ToString() ?? string.Empty;
                    var petModelID = jsonObject["PetModelID"]?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(modelID))
                    {
                        ModelIDs[ModelTarget.Character] = modelID;
                        ModLogger.Log($"Migrated ModelID ({modelID}) to ModelIDs dictionary");
                    }

                    if (!string.IsNullOrEmpty(petModelID))
                    {
                        ModelIDs[ModelTarget.Pet] = petModelID;
                        ModLogger.Log($"Migrated PetModelID ({petModelID}) to ModelIDs dictionary");
                    }

                    if (string.IsNullOrEmpty(modelID) && string.IsNullOrEmpty(petModelID)) return;
                    ModLogger.Log(
                        "Migration from old format (ModelID/PetModelID) to new format (ModelIDs) completed");
                    if (autoSaveOnLoad) SaveToFile(filePath);
                }
                else
                {
                    JsonConvert.PopulateObject(json, this, Constant.JsonSettings);
                    if (Validate() && autoSaveOnLoad) SaveToFile(filePath);
                }
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load config from file '{filePath}': {ex.Message}");
                LoadDefault();
                if (autoSaveOnLoad) SaveToFile(filePath);
            }
        }
    }
}