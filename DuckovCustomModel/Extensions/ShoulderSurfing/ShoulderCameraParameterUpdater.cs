using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Extensions.ShoulderSurfing
{
    public class ShoulderCameraParameterUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (control.CharacterMainControl == null || !control.CharacterMainControl.IsMainCharacter)
                return;

            var cameraPitch = ShoulderCameraCompat.GetCameraPitch();
            control.SetParameterFloat(CustomAnimatorHash.ModShoulderSurfingCameraPitch, cameraPitch);
        }
    }
}
