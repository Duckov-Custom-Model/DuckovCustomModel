using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.Localizations;
using SodaCraft.Localizations;
using UnityEngine;

namespace DuckovCustomModel.Managers
{
    public static class ModelTargetTypeRegistryExtensions
    {
        public static string GetDisplayName(string targetTypeId, SystemLanguage language = SystemLanguage.English)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return string.Empty;

            if (ModelTargetType.IsBuiltIn(targetTypeId))
                return GetBuiltInDisplayName(targetTypeId);

            var targetType = ModelTargetTypeRegistry.GetTargetType(targetTypeId);
            return targetType != null
                ? targetType.GetDisplayName(language)
                : targetTypeId;
        }

        private static string GetBuiltInDisplayName(string targetTypeId)
        {
            switch (targetTypeId)
            {
                case ModelTargetType.Character:
                    return Localization.TargetCharacter;
                case ModelTargetType.Pet:
                    return Localization.TargetPet;
                case ModelTargetType.AllAICharacters:
                    return Localization.TargetAllAICharacters;
            }

            if (!ModelTargetType.IsAICharacterTargetType(targetTypeId)) return targetTypeId;
            var aiName = ModelTargetType.ExtractAICharacterName(targetTypeId);
            return string.IsNullOrEmpty(aiName) ? targetTypeId : LocalizationManager.GetPlainText(aiName);
        }
    }
}
