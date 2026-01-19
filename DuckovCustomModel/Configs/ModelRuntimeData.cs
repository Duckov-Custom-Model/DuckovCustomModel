using System;
using System.Collections.Generic;

namespace DuckovCustomModel.Configs
{
    public class ModelRuntimeData : ConfigBase
    {
        public Dictionary<string, object> Data { get; set; } = [];

        public T? GetValue<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return default;
            if (!Data.TryGetValue(key, out var value)) return default;

            try
            {
                if (value is T typedValue)
                    return typedValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            if (value == null)
                Data.Remove(key);
            else
                Data[key] = value;
        }

        public bool RemoveValue(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && Data.Remove(key);
        }

        public override void LoadDefault()
        {
            Data.Clear();
        }

        public override bool Validate()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Data != null) return false;
            Data = [];
            return true;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not ModelRuntimeData otherData) return;
            Data = new(otherData.Data);
        }
    }
}
