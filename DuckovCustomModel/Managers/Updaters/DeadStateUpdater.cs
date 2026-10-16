using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class DeadStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;
            var isDead = control.CharacterMainControl.Health != null &&
                         control.CharacterMainControl.Health.IsDead;
            control.SetParameterBool(CustomAnimatorHash.Die, isDead);
        }
    }
}
