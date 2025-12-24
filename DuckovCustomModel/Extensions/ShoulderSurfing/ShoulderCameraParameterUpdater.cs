using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Extensions.ShoulderSurfing
{
    public class ShoulderCameraParameterUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;

            var cameraPitch = ShoulderCameraCompat.GetCameraPitch();
            customControl.SetParameterFloat(CustomAnimatorHash.ModShoulderSurfingCameraPitch, cameraPitch);
        }
    }
}
