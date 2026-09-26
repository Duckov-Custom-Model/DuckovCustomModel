using System;
using System.Collections.Generic;
using DuckovCustomModel.Managers;
using Newtonsoft.Json;

namespace DuckovCustomModel.Configs
{
    internal static class YsmFormSettings
    {
        private const string DataKey = "YsmFormValues";

        public static Dictionary<string, double> Load(string targetTypeId, string modelId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelId))
                return new(StringComparer.Ordinal);
            var data = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelId);
            var json = data.GetValue<string>(DataKey);
            if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.Ordinal);
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, double>>(json)
                       ?? new Dictionary<string, double>(StringComparer.Ordinal);
            }
            catch (Exception exception)
            {
                ModLogger.LogWarning($"YSM form settings for '{modelId}': {exception.Message}");
                return new(StringComparer.Ordinal);
            }
        }

        public static void Save(string targetTypeId, string modelId, IReadOnlyDictionary<string, double> values)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId) || string.IsNullOrWhiteSpace(modelId)) return;
            var data = ModelRuntimeDataManager.LoadRuntimeData(targetTypeId, modelId);
            if (values.Count == 0)
            {
                if (!data.RemoveValue(DataKey)) return;
                if (data.Data.Count == 0)
                {
                    ModelRuntimeDataManager.ClearRuntimeData(targetTypeId, modelId);
                    return;
                }
            }
            else
            {
                data.SetValue(DataKey, JsonConvert.SerializeObject(values));
            }

            ModelRuntimeDataManager.SaveRuntimeData(targetTypeId, modelId, data);
        }
    }
}
