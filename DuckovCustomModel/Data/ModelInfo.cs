using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DuckovCustomModel.Data
{
    public class ModelInfo : IValidatable
    {
        public string ModelID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string PrefabPath { get; set; } = string.Empty;

        [JsonIgnore] public string BundleName { get; internal set; } = string.Empty;

        public ModelTarget[] Target { get; set; } = [ModelTarget.Character];

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(ModelID)) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (string.IsNullOrWhiteSpace(PrefabPath)) return false;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            var targets = new List<ModelTarget>();
            foreach (var target in Target ?? [])
            {
                if (!Enum.IsDefined(typeof(ModelTarget), target)) continue;
                if (!targets.Contains(target)) targets.Add(target);
            }

            Target = targets.ToArray();

            return true;
        }

        public bool CompatibleWithType(ModelTarget modelTarget)
        {
            return Array.Exists(Target, target => target == modelTarget);
        }
    }
}