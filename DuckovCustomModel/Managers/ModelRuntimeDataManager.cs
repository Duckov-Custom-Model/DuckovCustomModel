using System;
using System.IO;
using System.Linq;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Managers
{
    public static class ModelRuntimeDataManager
    {
        private static string RuntimeDataDirectory => Path.Combine(ConfigManager.ConfigBaseDirectory, "RuntimeData");

        private static string GetTargetTypeDirectory(string targetTypeId)
        {
            var safeTargetType = string.Join("_", targetTypeId.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(RuntimeDataDirectory, safeTargetType);
        }

        private static string GetConfigFileName(string modelID)
        {
            var safeModelID = string.Join("_", modelID.Split(Path.GetInvalidFileNameChars()));
            return $"{safeModelID}.json";
        }

        private static string GetConfigFilePath(string targetTypeId, string modelID)
        {
            var targetDir = GetTargetTypeDirectory(targetTypeId);
            return Path.Combine(targetDir, GetConfigFileName(modelID));
        }

        private static void EnsureTargetTypeDirectoryExists(string targetTypeId)
        {
            var targetDir = GetTargetTypeDirectory(targetTypeId);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
        }

        public static ModelRuntimeData LoadRuntimeData(string targetTypeId, string modelID)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
            {
                ModLogger.LogWarning("TargetTypeId or ModelID is empty, cannot load runtime data.");
                return new();
            }

            if (targetTypeId.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase))
                return new();

            try
            {
                EnsureTargetTypeDirectoryExists(targetTypeId);
                var filePath = GetConfigFilePath(targetTypeId, modelID);

                if (!File.Exists(filePath)) return new();

                var runtimeData = new ModelRuntimeData();
                runtimeData.LoadFromFile(filePath, false);
                return runtimeData;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load runtime data for {targetTypeId}/{modelID}: {ex.Message}");
                return new();
            }
        }

        public static void SaveRuntimeData(string targetTypeId, string modelID, ModelRuntimeData runtimeData)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelID))
            {
                ModLogger.LogWarning("TargetTypeId or ModelID is empty, cannot save runtime data.");
                return;
            }

            if (targetTypeId.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase))
                return;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (runtimeData == null)
            {
                ModLogger.LogWarning("Runtime data is null, cannot save.");
                return;
            }

            try
            {
                EnsureTargetTypeDirectoryExists(targetTypeId);
                var filePath = GetConfigFilePath(targetTypeId, modelID);
                runtimeData.SaveToFile(filePath, false);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to save runtime data for {targetTypeId}/{modelID}: {ex.Message}");
            }
        }

        public static bool ClearRuntimeData(string targetTypeId, string? modelID = null)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId))
            {
                ModLogger.LogWarning("TargetTypeId is empty, cannot clear runtime data.");
                return false;
            }

            if (targetTypeId.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var targetDir = GetTargetTypeDirectory(targetTypeId);

                if (!Directory.Exists(targetDir))
                    return false;

                if (string.IsNullOrWhiteSpace(modelID))
                {
                    Directory.Delete(targetDir, true);
                    ModLogger.Log($"Cleared all runtime data for target type: {targetTypeId}");
                    return true;
                }

                var filePath = GetConfigFilePath(targetTypeId, modelID);

                if (!File.Exists(filePath))
                    return false;

                File.Delete(filePath);
                ModLogger.Log($"Cleared runtime data for {targetTypeId}/{modelID}");

                if (Directory.GetFiles(targetDir).Length == 0)
                    Directory.Delete(targetDir);

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to clear runtime data for {targetTypeId}/{modelID ?? "all"}: {ex.Message}");
                return false;
            }
        }

        public static void ClearAllRuntimeData()
        {
            try
            {
                if (!Directory.Exists(RuntimeDataDirectory))
                    return;

                Directory.Delete(RuntimeDataDirectory, true);
                ModLogger.Log("Cleared all runtime data.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to clear all runtime data: {ex.Message}");
            }
        }

        public static string[] GetAllTargetTypes()
        {
            try
            {
                if (!Directory.Exists(RuntimeDataDirectory))
                    return [];

                return Directory.GetDirectories(RuntimeDataDirectory)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to get all target types: {ex.Message}");
                return [];
            }
        }

        public static string[] GetModelIDsForTargetType(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId))
                return [];

            try
            {
                var targetDir = GetTargetTypeDirectory(targetTypeId);

                if (!Directory.Exists(targetDir))
                    return [];

                return Directory.GetFiles(targetDir, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to get model IDs for target type {targetTypeId}: {ex.Message}");
                return [];
            }
        }
    }
}
