using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;
using UnityEngine;

namespace DuckovCustomModel.Configs
{
    public class IdleAudioInterval
    {
        public float Min { get; set; } = 30f;
        public float Max { get; set; } = 45f;
    }

    public class IdleAudioConfig : ConfigBase
    {
        public int Version { get; set; } = 2;

        public Dictionary<string, IdleAudioInterval> TargetTypeIdleAudioIntervals { get; set; } = [];
        public Dictionary<string, bool> TargetTypeEnableIdleAudio { get; set; } = [];

        public override void LoadDefault()
        {
            Version = 2;
            TargetTypeIdleAudioIntervals = [];
            TargetTypeIdleAudioIntervals[ModelTargetType.Character] = new() { Min = 30f, Max = 45f };
            TargetTypeIdleAudioIntervals[ModelTargetType.Pet] = new() { Min = 30f, Max = 45f };

            TargetTypeEnableIdleAudio = [];
            TargetTypeEnableIdleAudio[ModelTargetType.Character] = false;
            TargetTypeEnableIdleAudio[ModelTargetType.Pet] = true;
            TargetTypeEnableIdleAudio[ModelTargetType.AllAICharacters] = true;
        }

        public override bool Validate()
        {
            var modified = false;

            if (Version < 2)
            {
                MigrateToVersion2();
                Version = 2;
                modified = true;
            }

            TargetTypeIdleAudioIntervals ??= [];
            TargetTypeEnableIdleAudio ??= [];

            var builtInTargetTypes = new[] { ModelTargetType.Character, ModelTargetType.Pet };
            foreach (var targetTypeId in builtInTargetTypes)
            {
                if (!TargetTypeIdleAudioIntervals.TryGetValue(targetTypeId, out var interval) || interval == null)
                {
                    TargetTypeIdleAudioIntervals[targetTypeId] = new() { Min = 30f, Max = 45f };
                    modified = true;
                }
                else
                {
                    var originalMin = interval.Min;
                    var originalMax = interval.Max;
                    if (interval.Min < 0.1f) interval.Min = 0.1f;
                    if (interval.Max < interval.Min) interval.Max = interval.Min;
                    if (!Mathf.Approximately(originalMin, interval.Min) ||
                        !Mathf.Approximately(originalMax, interval.Max))
                        modified = true;
                }

                if (TargetTypeEnableIdleAudio.ContainsKey(targetTypeId)) continue;
                TargetTypeEnableIdleAudio[targetTypeId] = targetTypeId != ModelTargetType.Character;
                modified = true;
            }

            if (TargetTypeEnableIdleAudio.TryAdd(ModelTargetType.AllAICharacters, true)) modified = true;

#pragma warning disable CS0618
            AICharacterIdleAudioIntervals ??= [];
            foreach (var (key, interval) in AICharacterIdleAudioIntervals)
                if (interval == null)
                {
                    AICharacterIdleAudioIntervals[key] = new() { Min = 30f, Max = 45f };
                    modified = true;
                }
                else
                {
                    var originalMin = interval.Min;
                    var originalMax = interval.Max;
                    if (interval.Min < 0.1f) interval.Min = 0.1f;
                    if (interval.Max < interval.Min) interval.Max = interval.Min;
                    if (!Mathf.Approximately(originalMin, interval.Min) ||
                        !Mathf.Approximately(originalMax, interval.Max))
                        modified = true;
                }

            AICharacterEnableIdleAudio ??= [];
            if (!AICharacterEnableIdleAudio.TryAdd(AICharacters.AllAICharactersKey, true)) return modified;
            modified = true;
#pragma warning restore CS0618

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not IdleAudioConfig otherConfig) return;
            Version = otherConfig.Version;
            TargetTypeIdleAudioIntervals = new(otherConfig.TargetTypeIdleAudioIntervals);
            TargetTypeEnableIdleAudio = new(otherConfig.TargetTypeEnableIdleAudio);
#pragma warning disable CS0618
            IdleAudioIntervals = new(otherConfig.IdleAudioIntervals);
            AICharacterIdleAudioIntervals = new(otherConfig.AICharacterIdleAudioIntervals);
            EnableIdleAudio = new(otherConfig.EnableIdleAudio);
            AICharacterEnableIdleAudio = new(otherConfig.AICharacterEnableIdleAudio);
#pragma warning restore CS0618
        }

        private void MigrateToVersion2()
        {
            TargetTypeIdleAudioIntervals ??= [];
            TargetTypeEnableIdleAudio ??= [];

#pragma warning disable CS0618
            foreach (var (target, interval) in IdleAudioIntervals)
            {
                if (interval == null) continue;
                var targetTypeId = target.ToTargetTypeId();
                TargetTypeIdleAudioIntervals[targetTypeId] = interval;
            }

            foreach (var (target, enabled) in EnableIdleAudio)
            {
                var targetTypeId = target.ToTargetTypeId();
                TargetTypeEnableIdleAudio[targetTypeId] = enabled;
            }

            foreach (var (nameKey, interval) in AICharacterIdleAudioIntervals)
            {
                if (interval == null) continue;
                if (nameKey == AICharacters.AllAICharactersKey)
                {
                    TargetTypeIdleAudioIntervals[ModelTargetType.AllAICharacters] = interval;
                }
                else
                {
                    var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
                    TargetTypeIdleAudioIntervals[targetTypeId] = interval;
                }
            }

            foreach (var (nameKey, enabled) in AICharacterEnableIdleAudio)
                if (nameKey == AICharacters.AllAICharactersKey)
                {
                    TargetTypeEnableIdleAudio[ModelTargetType.AllAICharacters] = enabled;
                }
                else
                {
                    var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
                    TargetTypeEnableIdleAudio[targetTypeId] = enabled;
                }
#pragma warning restore CS0618
        }

        public IdleAudioInterval GetIdleAudioInterval(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return new() { Min = 30f, Max = 45f };
            return TargetTypeIdleAudioIntervals.TryGetValue(targetTypeId, out var value) && value != null
                ? value
                : new() { Min = 30f, Max = 45f };
        }

        public void SetIdleAudioInterval(string targetTypeId, float min, float max)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;
            if (min < 0.1f) min = 0.1f;
            if (max < min) max = min;
            TargetTypeIdleAudioIntervals[targetTypeId] = new() { Min = min, Max = max };
        }

        public bool IsIdleAudioEnabled(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return false;
            return TargetTypeEnableIdleAudio.TryGetValue(targetTypeId, out var enabled) && enabled;
        }

        public void SetIdleAudioEnabled(string targetTypeId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;
            TargetTypeEnableIdleAudio[targetTypeId] = enabled;
        }

        #region 过时成员（向后兼容）

        [Obsolete("Use TargetTypeIdleAudioIntervals instead. This property is kept for backward compatibility.")]
        public Dictionary<ModelTarget, IdleAudioInterval> IdleAudioIntervals { get; set; } = [];

        [Obsolete("Use TargetTypeIdleAudioIntervals instead. This property is kept for backward compatibility.")]
        public Dictionary<string, IdleAudioInterval> AICharacterIdleAudioIntervals { get; set; } = [];

        [Obsolete("Use TargetTypeEnableIdleAudio instead. This property is kept for backward compatibility.")]
        public Dictionary<ModelTarget, bool> EnableIdleAudio { get; set; } = [];

        [Obsolete("Use TargetTypeEnableIdleAudio instead. This property is kept for backward compatibility.")]
        public Dictionary<string, bool> AICharacterEnableIdleAudio { get; set; } = [];

        [Obsolete("Use GetIdleAudioInterval(string targetTypeId) instead.")]
        public IdleAudioInterval GetIdleAudioInterval(ModelTarget target)
        {
            var targetTypeId = target.ToTargetTypeId();
            return GetIdleAudioInterval(targetTypeId);
        }

        [Obsolete("Use SetIdleAudioInterval(string targetTypeId, float min, float max) instead.")]
        public void SetIdleAudioInterval(ModelTarget target, float min, float max)
        {
            var targetTypeId = target.ToTargetTypeId();
            SetIdleAudioInterval(targetTypeId, min, max);
        }

        [Obsolete(
            "Use GetIdleAudioInterval(string targetTypeId) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public IdleAudioInterval GetAICharacterIdleAudioInterval(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return GetIdleAudioInterval(ModelTargetType.AllAICharacters);

            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            var interval = GetIdleAudioInterval(targetTypeId);
            if (!Mathf.Approximately(interval.Min, 30f) || !Mathf.Approximately(interval.Max, 45f)) return interval;

            var fallbackInterval = GetIdleAudioInterval(ModelTargetType.AllAICharacters);
            return fallbackInterval;
        }

        [Obsolete(
            "Use SetIdleAudioInterval(string targetTypeId, float min, float max) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public void SetAICharacterIdleAudioInterval(string nameKey, float min, float max)
        {
            if (string.IsNullOrEmpty(nameKey)) return;
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            SetIdleAudioInterval(targetTypeId, min, max);
        }

        [Obsolete("Use IsIdleAudioEnabled(string targetTypeId) instead.")]
        public bool IsIdleAudioEnabled(ModelTarget target)
        {
            var targetTypeId = target.ToTargetTypeId();
            return IsIdleAudioEnabled(targetTypeId);
        }

        [Obsolete("Use SetIdleAudioEnabled(string targetTypeId, bool enabled) instead.")]
        public void SetIdleAudioEnabled(ModelTarget target, bool enabled)
        {
            var targetTypeId = target.ToTargetTypeId();
            SetIdleAudioEnabled(targetTypeId, enabled);
        }

        [Obsolete(
            "Use IsIdleAudioEnabled(string targetTypeId) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public bool IsAICharacterIdleAudioEnabled(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return IsIdleAudioEnabled(ModelTargetType.AllAICharacters);

            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            return TargetTypeEnableIdleAudio.TryGetValue(targetTypeId, out var enabled)
                ? enabled
                : IsIdleAudioEnabled(ModelTargetType.AllAICharacters);
        }

        [Obsolete(
            "Use SetIdleAudioEnabled(string targetTypeId, bool enabled) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public void SetAICharacterIdleAudioEnabled(string nameKey, bool enabled)
        {
            if (string.IsNullOrEmpty(nameKey)) return;
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            SetIdleAudioEnabled(targetTypeId, enabled);
        }

        #endregion
    }
}
