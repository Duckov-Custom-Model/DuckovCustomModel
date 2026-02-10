using System;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using UnityEngine;

namespace DuckovCustomModel.UI.Data
{
    public class TargetInfo
    {
        public string Id { get; set; } = string.Empty;
        public string TargetTypeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

        public string UsingModel
        {
            get
            {
                if (ModEntry.UsingModel == null)
                    return string.Empty;

                var targetTypeId = GetTargetTypeId();
                var modelID = ModEntry.UsingModel.GetModelID(targetTypeId);
                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                return modelID ?? string.Empty;
            }
        }

        public string UsingFallbackModel
        {
            get
            {
                if (ModEntry.UsingModel == null || !IsAICharacter())
                    return string.Empty;

                var fallbackModelID = ModEntry.UsingModel.GetModelID(ModelTargetType.AllAICharacters);
                // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
                return fallbackModelID ?? string.Empty;
            }
        }

        public bool HasModel => !string.IsNullOrWhiteSpace(UsingModel);

        public bool HasFallbackModel => !HasModel && !string.IsNullOrWhiteSpace(UsingFallbackModel);

        public static TargetInfo CreateCharacterTarget()
        {
            var targetInfo = new TargetInfo
            {
                Id = "Character",
                TargetTypeId = ModelTargetType.Character,
                DisplayName = Localization.TargetCharacter,
            };
            InitializeObsoleteProperties(targetInfo);
            return targetInfo;
        }

        public static TargetInfo CreatePetTarget()
        {
            var targetInfo = new TargetInfo
            {
                Id = "Pet",
                TargetTypeId = ModelTargetType.Pet,
                DisplayName = Localization.TargetPet,
            };
            InitializeObsoleteProperties(targetInfo);
            return targetInfo;
        }

        public static TargetInfo CreateAICharacterTarget(string nameKey, string displayName)
        {
            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            var targetInfo = new TargetInfo
            {
                Id = $"AI_{nameKey}",
                TargetTypeId = targetTypeId,
                DisplayName = displayName,
            };
            InitializeObsoleteProperties(targetInfo);
            return targetInfo;
        }

        public static TargetInfo CreateAllAICharactersTarget()
        {
            var targetInfo = new TargetInfo
            {
                Id = "AI_*",
                TargetTypeId = ModelTargetType.AllAICharacters,
                DisplayName = Localization.TargetAllAICharacters,
            };
            InitializeObsoleteProperties(targetInfo);
            return targetInfo;
        }

        public static TargetInfo CreateFromTargetTypeId(string targetTypeId,
            SystemLanguage language = SystemLanguage.English)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId))
                throw new ArgumentException("Target type ID cannot be null or empty.", nameof(targetTypeId));

            var displayName = ModelTargetTypeRegistryExtensions.GetDisplayName(targetTypeId, language);
            var id = targetTypeId.Replace(ModelTargetType.BuiltInPrefix, "")
                .Replace(ModelTargetType.ExtensionPrefix, "");

            var targetInfo = new TargetInfo
            {
                Id = id,
                TargetTypeId = targetTypeId,
                DisplayName = displayName,
            };
            InitializeObsoleteProperties(targetInfo);
            return targetInfo;
        }

        private static void InitializeObsoleteProperties(TargetInfo targetInfo)
        {
#pragma warning disable CS0618
            var target = ModelTargetExtensions.FromTargetTypeId(targetInfo.TargetTypeId);
            if (!target.HasValue) return;
            targetInfo.TargetType = target.Value;
            if (target.Value != ModelTarget.AICharacter) return;
            var aiName = ModelTargetType.ExtractAICharacterName(targetInfo.TargetTypeId);
            targetInfo.AICharacterNameKey = aiName;
#pragma warning restore CS0618
        }

        public string GetTargetTypeId()
        {
            if (!string.IsNullOrWhiteSpace(TargetTypeId))
                return TargetTypeId;

#pragma warning disable CS0618
            var targetTypeId = TargetType.ToTargetTypeId();
            if (TargetType == ModelTarget.AICharacter && !string.IsNullOrEmpty(AICharacterNameKey))
                targetTypeId = ModelTargetType.CreateAICharacterTargetType(AICharacterNameKey);
#pragma warning restore CS0618

            return targetTypeId;
        }

        public string? GetAICharacterNameKey()
        {
            var targetTypeId = GetTargetTypeId();
#pragma warning disable CS0618
            return ModelTargetType.IsAICharacterTargetType(targetTypeId)
                ? ModelTargetType.ExtractAICharacterName(targetTypeId)
                : AICharacterNameKey;
#pragma warning restore CS0618
        }

        public bool IsAICharacter()
        {
            return ModelTargetType.IsAICharacterTargetType(GetTargetTypeId());
        }

        public bool IsExtension()
        {
            return ModelTargetType.IsExtension(GetTargetTypeId());
        }

        #region 过时成员（向后兼容）

        [Obsolete("Use TargetTypeId instead. This property is kept for backward compatibility.")]
        public ModelTarget TargetType { get; set; }

        [Obsolete("Use TargetTypeId instead. This property is kept for backward compatibility.")]
        public string? AICharacterNameKey { get; set; }

        #endregion
    }
}
