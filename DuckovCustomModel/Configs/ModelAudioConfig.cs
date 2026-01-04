using System;
using System.Collections.Generic;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Configs
{
    public class ModelAudioConfig : ConfigBase
    {
        public int Version { get; set; } = 2;

        public Dictionary<string, bool> TargetTypeEnableModelAudio { get; set; } = [];
        public Dictionary<string, float> TargetTypeModelAudioVolume { get; set; } = [];

        public override void LoadDefault()
        {
            Version = 2;
            TargetTypeEnableModelAudio = [];
            TargetTypeEnableModelAudio[ModelTargetType.Character] = true;
            TargetTypeEnableModelAudio[ModelTargetType.Pet] = true;
            TargetTypeEnableModelAudio[ModelTargetType.AllAICharacters] = true;

            TargetTypeModelAudioVolume = [];
            TargetTypeModelAudioVolume[ModelTargetType.Character] = 1f;
            TargetTypeModelAudioVolume[ModelTargetType.Pet] = 1f;
            TargetTypeModelAudioVolume[ModelTargetType.AllAICharacters] = 1f;
        }

        public override bool Validate()
        {
            var modified = false;

#pragma warning disable CS0618
            EnableModelAudio ??= [];
            AICharacterEnableModelAudio ??= [];

            var hasOldData = EnableModelAudio.Count > 0 || AICharacterEnableModelAudio.Count > 0;
            var hasNewData = TargetTypeEnableModelAudio.Count > 0 || TargetTypeModelAudioVolume.Count > 0;
            var needsMigration = Version < 2 || (Version >= 2 && hasOldData && !hasNewData);
#pragma warning restore CS0618

            if (needsMigration)
            {
                MigrateToVersion2();
                Version = 2;
                modified = true;
            }

            TargetTypeEnableModelAudio ??= [];
            TargetTypeModelAudioVolume ??= [];

            var builtInTargetTypes = new[]
            {
                ModelTargetType.Character,
                ModelTargetType.Pet,
                ModelTargetType.AllAICharacters,
            };
            foreach (var targetTypeId in builtInTargetTypes)
            {
                if (TargetTypeEnableModelAudio.TryAdd(targetTypeId, true))
                    modified = true;
                if (TargetTypeModelAudioVolume.TryAdd(targetTypeId, 1f))
                    modified = true;
            }

            return modified;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not ModelAudioConfig otherConfig) return;
            Version = otherConfig.Version;
            TargetTypeEnableModelAudio = new(otherConfig.TargetTypeEnableModelAudio);
            TargetTypeModelAudioVolume = new(otherConfig.TargetTypeModelAudioVolume);
#pragma warning disable CS0618
            EnableModelAudio = new(otherConfig.EnableModelAudio);
            AICharacterEnableModelAudio = new(otherConfig.AICharacterEnableModelAudio);
#pragma warning restore CS0618
        }

        private void MigrateToVersion2()
        {
            TargetTypeEnableModelAudio ??= [];

#pragma warning disable CS0618
            foreach (var (target, enabled) in EnableModelAudio)
            {
                var targetTypeId = target.ToTargetTypeId();
                TargetTypeEnableModelAudio[targetTypeId] = enabled;
            }

            foreach (var (nameKey, enabled) in AICharacterEnableModelAudio)
                if (nameKey == AICharacters.AllAICharactersKey)
                {
                    TargetTypeEnableModelAudio[ModelTargetType.AllAICharacters] = enabled;
                }
                else
                {
                    var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
                    TargetTypeEnableModelAudio[targetTypeId] = enabled;
                }
#pragma warning restore CS0618
        }

        public bool IsModelAudioEnabled(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return true;
            return TargetTypeEnableModelAudio.TryGetValue(targetTypeId, out var enabled) && enabled;
        }

        public void SetModelAudioEnabled(string targetTypeId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;
            TargetTypeEnableModelAudio[targetTypeId] = enabled;
        }

        public float GetModelAudioVolume(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return 1f;
            if (TargetTypeModelAudioVolume.TryGetValue(targetTypeId, out var volume))
                return volume;
            return ModelTargetType.IsAICharacterTargetType(targetTypeId)
                ? TargetTypeModelAudioVolume.GetValueOrDefault(ModelTargetType.AllAICharacters, 1f)
                : 1f;
        }

        public void SetModelAudioVolume(string targetTypeId, float volume)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;
            TargetTypeModelAudioVolume[targetTypeId] = Math.Clamp(volume, 0f, 1f);
        }

        #region 过时成员（向后兼容）

        [Obsolete("Use TargetTypeEnableModelAudio instead. This property is kept for backward compatibility.")]
        public Dictionary<ModelTarget, bool> EnableModelAudio { get; set; } = [];

        [Obsolete("Use TargetTypeEnableModelAudio instead. This property is kept for backward compatibility.")]
        public Dictionary<string, bool> AICharacterEnableModelAudio { get; set; } = [];

        [Obsolete("Use IsModelAudioEnabled(string targetTypeId) instead.")]
        public bool IsModelAudioEnabled(ModelTarget target)
        {
            var targetTypeId = target.ToTargetTypeId();
            return IsModelAudioEnabled(targetTypeId);
        }

        [Obsolete("Use SetModelAudioEnabled(string targetTypeId, bool enabled) instead.")]
        public void SetModelAudioEnabled(ModelTarget target, bool enabled)
        {
            var targetTypeId = target.ToTargetTypeId();
            SetModelAudioEnabled(targetTypeId, enabled);
        }

        [Obsolete(
            "Use IsModelAudioEnabled(string targetTypeId) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public bool IsAICharacterModelAudioEnabled(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return IsModelAudioEnabled(ModelTargetType.AllAICharacters);

            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            return TargetTypeEnableModelAudio.TryGetValue(targetTypeId, out var enabled)
                ? enabled
                : IsModelAudioEnabled(ModelTargetType.AllAICharacters);
        }

        [Obsolete(
            "Use SetModelAudioEnabled(string targetTypeId, bool enabled) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public void SetAICharacterModelAudioEnabled(string nameKey, bool enabled)
        {
            if (string.IsNullOrEmpty(nameKey)) return;
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            SetModelAudioEnabled(targetTypeId, enabled);
        }

        #endregion
    }
}
