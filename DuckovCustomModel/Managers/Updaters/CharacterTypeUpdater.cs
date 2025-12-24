using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class CharacterTypeUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.ModelHandler == null) return;

            var characterType = (int)ctx.ModelHandler.Target;
            customControl.SetParameterInteger(CustomAnimatorHash.CurrentCharacterType, characterType);
        }
    }
}
