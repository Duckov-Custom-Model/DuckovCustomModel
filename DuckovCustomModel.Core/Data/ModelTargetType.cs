using System;

namespace DuckovCustomModel.Core.Data
{
    public static class ModelTargetType
    {
        public const string BuiltInPrefix = "built-in:";
        public const string ExtensionPrefix = "extension:";

        public const string Character = BuiltInPrefix + "Character";
        public const string Pet = BuiltInPrefix + "Pet";
        public const string AICharacterTemplate = BuiltInPrefix + "AICharacter_";
        public const string AllAICharacters = AICharacterTemplate + "*";

        public static string CreateAICharacterTargetType(string aiCharacterName)
        {
            return string.IsNullOrWhiteSpace(aiCharacterName)
                ? throw new ArgumentException("AI character name cannot be null or empty.", nameof(aiCharacterName))
                : $"{BuiltInPrefix}AICharacter_{aiCharacterName}";
        }

        public static bool IsBuiltIn(string targetTypeId)
        {
            return !string.IsNullOrEmpty(targetTypeId) && targetTypeId.StartsWith(BuiltInPrefix);
        }

        public static bool IsExtension(string targetTypeId)
        {
            return !string.IsNullOrEmpty(targetTypeId) && targetTypeId.StartsWith(ExtensionPrefix);
        }

        public static bool IsValid(string targetTypeId)
        {
            return IsBuiltIn(targetTypeId) || IsExtension(targetTypeId);
        }

        public static string? ExtractAICharacterName(string targetTypeId)
        {
            if (string.IsNullOrEmpty(targetTypeId)) return null;
            return !targetTypeId.StartsWith(AICharacterTemplate)
                ? null
                : targetTypeId[AICharacterTemplate.Length..];
        }

        public static bool IsAICharacterTargetType(string targetTypeId)
        {
            return !string.IsNullOrEmpty(targetTypeId) &&
                   targetTypeId.StartsWith(AICharacterTemplate);
        }
    }
}
