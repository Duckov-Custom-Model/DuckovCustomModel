using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using UnityEngine;

namespace DuckovCustomModel.Managers.Updaters
{
    public class CharacterTypeUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.ModelHandler == null) return;

            var targetTypeId = control.ModelHandler.GetTargetTypeId();
            var characterType = GetCharacterTypeFromTargetTypeId(targetTypeId);
            control.SetParameterInteger(CustomAnimatorHash.CurrentCharacterType, characterType);

            if (characterType == -1)
            {
                var customTypeID = GetCustomTypeIDFromTargetTypeId(targetTypeId);
                control.SetParameterInteger(CustomAnimatorHash.CustomCharacterTypeID, customTypeID);
            }
            else
            {
                control.SetParameterInteger(CustomAnimatorHash.CustomCharacterTypeID, 0);
            }
        }

        private static int GetCharacterTypeFromTargetTypeId(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return 0;
            if (ModelTargetType.IsExtension(targetTypeId)) return -1;

            return targetTypeId switch
            {
                ModelTargetType.Character => 0,
                ModelTargetType.Pet => 1,
                _ => ModelTargetType.IsAICharacterTargetType(targetTypeId) ? 2 : 0,
            };
        }

        private static int GetCustomTypeIDFromTargetTypeId(string targetTypeId)
        {
            return string.IsNullOrWhiteSpace(targetTypeId)
                ? 0
                : Animator.StringToHash(targetTypeId);
        }
    }
}
