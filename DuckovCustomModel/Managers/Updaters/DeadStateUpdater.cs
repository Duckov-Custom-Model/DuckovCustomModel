using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class DeadStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;
            if (ctx.CharacterMainControl.Health == null) return;

            var isDead = ctx.CharacterMainControl.Health.IsDead;
            customControl.SetParameterBool(CustomAnimatorHash.Die, isDead);
        }
    }
}
