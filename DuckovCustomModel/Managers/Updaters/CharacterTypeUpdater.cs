using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class CharacterTypeUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.ModelHandler == null) return;

            var characterType = (int)control.ModelHandler.Target;
            control.SetParameterInteger(CustomAnimatorHash.CurrentCharacterType, characterType);
        }
    }
}
