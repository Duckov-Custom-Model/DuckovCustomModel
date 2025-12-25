using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class DeadStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;
            if (control.CharacterMainControl.Health == null) return;

            var isDead = control.CharacterMainControl.Health.IsDead;
            control.SetParameterBool(CustomAnimatorHash.Die, isDead);
        }
    }
}
