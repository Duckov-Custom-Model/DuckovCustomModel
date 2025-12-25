using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class VelocityAndAimUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var velocity = control.CharacterMainControl.Velocity;
            control.SetParameterFloat(CustomAnimatorHash.VelocityMagnitude, velocity.magnitude);
            control.SetParameterFloat(CustomAnimatorHash.VelocityX, velocity.x);
            control.SetParameterFloat(CustomAnimatorHash.VelocityY, velocity.y);
            control.SetParameterFloat(CustomAnimatorHash.VelocityZ, velocity.z);

            var aimDir = control.CharacterMainControl.CurrentAimDirection;
            control.SetParameterFloat(CustomAnimatorHash.AimDirX, aimDir.x);
            control.SetParameterFloat(CustomAnimatorHash.AimDirY, aimDir.y);
            control.SetParameterFloat(CustomAnimatorHash.AimDirZ, aimDir.z);

            var inAds = control.CharacterMainControl.IsInAdsInput;
            control.SetParameterBool(CustomAnimatorHash.InAds, inAds);

            var adsValue = control.CharacterMainControl.AdsValue;
            control.SetParameterFloat(CustomAnimatorHash.AdsValue, adsValue);

            var aimType = (int)control.CharacterMainControl.AimType;
            control.SetParameterInteger(CustomAnimatorHash.AimType, aimType);
        }
    }
}
