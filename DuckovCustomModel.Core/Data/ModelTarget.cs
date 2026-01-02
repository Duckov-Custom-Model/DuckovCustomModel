using System;

namespace DuckovCustomModel.Core.Data
{
    #region 过时成员（向后兼容）

    [Obsolete("Use ModelTargetType string identifiers instead. This enum is kept for backward compatibility.")]
    public enum ModelTarget
    {
        Character,
        Pet,
        AICharacter,
    }

    [Obsolete(
        "Use ModelTargetType string identifiers and related methods instead. This class is kept for backward compatibility.")]
    public static class ModelTargetExtensions
    {
        [Obsolete("Use ModelTargetType string identifiers directly instead.")]
        public static string ToTargetTypeId(this ModelTarget target, string? aiCharacterName = null)
        {
            return target switch
            {
                ModelTarget.Character => ModelTargetType.Character,
                ModelTarget.Pet => ModelTargetType.Pet,
                ModelTarget.AICharacter when !string.IsNullOrEmpty(aiCharacterName) =>
                    ModelTargetType.CreateAICharacterTargetType(aiCharacterName),
                ModelTarget.AICharacter => ModelTargetType.AllAICharacters,
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, null),
            };
        }

        [Obsolete("Use ModelTargetType string identifiers directly instead.")]
        public static ModelTarget? FromTargetTypeId(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return null;

            switch (targetTypeId)
            {
                case ModelTargetType.Character:
                    return ModelTarget.Character;
                case ModelTargetType.Pet:
                    return ModelTarget.Pet;
            }

            if (ModelTargetType.IsAICharacterTargetType(targetTypeId))
                return ModelTarget.AICharacter;

            return null;
        }
    }

    #endregion
}
