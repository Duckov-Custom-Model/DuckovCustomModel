using System;
using System.Collections.Generic;
using System.Globalization;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Core.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DuckovCustomModel.Managers
{
    public static class ModelSocketScaleManager
    {
        public const float DefaultScale = 1f;
        public const float MinScale = 0.1f;
        public const float MaxScale = 5f;
        private const string DataKey = "SocketScales";

        public static event Action<string, string>? OnScaleChanged;

        public static float GetScale(string targetTypeId, string modelID, string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return DefaultScale;
            var scales = GetScales(targetTypeId, modelID);
            return scales.TryGetValue(slot, out var scale) ? scale : DefaultScale;
        }

        public static Dictionary<string, float> GetScales(string targetTypeId, string modelID)
        {
            return ValidTarget(targetTypeId, modelID)
                ? ReadScales(ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelID))
                : new(StringComparer.Ordinal);
        }

        public static void SetScale(string targetTypeId, string modelID, string slot, float scale)
        {
            if (!ValidTarget(targetTypeId, modelID) || string.IsNullOrWhiteSpace(slot) || !ValidScale(scale)) return;
            var runtimeData = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelID);
            var scales = ReadScales(runtimeData);
            var previous = scales.TryGetValue(slot, out var stored) ? stored : DefaultScale;
            if (previous == scale) return;
            if (scale == DefaultScale) scales.Remove(slot);
            else scales[slot] = scale;
            Save(targetTypeId, modelID, runtimeData, scales);
        }

        public static void ResetScale(string targetTypeId, string modelID, string slot)
        {
            SetScale(targetTypeId, modelID, slot, DefaultScale);
        }

        public static void ResetAll(string targetTypeId, string modelID)
        {
            if (!ValidTarget(targetTypeId, modelID)) return;
            var runtimeData = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelID);
            if (!runtimeData.RemoveValue(DataKey)) return;
            ModelRuntimeDataManager.SaveRuntimeData(targetTypeId, modelID, runtimeData);
            OnScaleChanged?.Invoke(targetTypeId, modelID);
        }

        private static Dictionary<string, float> ReadScales(ModelRuntimeData runtimeData)
        {
            var scales = new Dictionary<string, float>(StringComparer.Ordinal);
            if (!runtimeData.Data.TryGetValue(DataKey, out var value)) return scales;
            if (value is IDictionary<string, float> stored)
            {
                foreach (var pair in stored)
                    if (!string.IsNullOrWhiteSpace(pair.Key) && ValidScale(pair.Value) && pair.Value != DefaultScale)
                        scales[pair.Key] = pair.Value;
            }
            else if (value is JObject json)
            {
                foreach (var property in json.Properties())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) ||
                        property.Value.Type is not (JTokenType.Integer or JTokenType.Float)) continue;
                    if (float.TryParse(property.Value.ToString(Formatting.None), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var scale) && ValidScale(scale) && scale != DefaultScale)
                        scales[property.Name] = scale;
                }
            }

            return scales;
        }

        private static void Save(string targetTypeId, string modelID, ModelRuntimeData runtimeData,
            Dictionary<string, float> scales)
        {
            if (scales.Count == 0) runtimeData.RemoveValue(DataKey);
            else runtimeData.SetValue(DataKey, scales);
            ModelRuntimeDataManager.SaveRuntimeData(targetTypeId, modelID, runtimeData);
            OnScaleChanged?.Invoke(targetTypeId, modelID);
        }

        private static bool ValidTarget(string targetTypeId, string modelID)
        {
            return !string.IsNullOrWhiteSpace(targetTypeId) && !string.IsNullOrWhiteSpace(modelID) &&
                   !targetTypeId.Equals(ModelTargetType.AllAICharacters, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidScale(float scale)
        {
            return !float.IsNaN(scale) && !float.IsInfinity(scale) && scale >= MinScale && scale <= MaxScale;
        }
    }
}
