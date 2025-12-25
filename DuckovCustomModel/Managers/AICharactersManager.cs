using System.Collections.Generic;
using System.Linq;
using Duckov.Utilities;
using DuckovCustomModel.Core.Data;
using UnityEngine;

namespace DuckovCustomModel.Managers
{
    public static class AICharactersManager
    {
        public static void Initialize()
        {
            CollectAllAICharacters();
        }

        public static void CollectAllAICharacters()
        {
            var presets = GameplayDataSettings.CharacterRandomPresetData.presets;
            var aiCharacterNameKeys = new HashSet<string>();

            foreach (var preset in presets.Where(preset => !string.IsNullOrEmpty(preset.nameKey))
                         .Where(preset => preset.nameKey != AICharacters.AllAICharactersKey))
            {
                aiCharacterNameKeys.Add(preset.nameKey);
            }

            if (aiCharacterNameKeys.Count <= 0) return;
            AICharacters.AddAICharacters(aiCharacterNameKeys);
            ModLogger.Log($"Collected {aiCharacterNameKeys.Count} AI character preset(s) from game resources");
        }
    }
}

