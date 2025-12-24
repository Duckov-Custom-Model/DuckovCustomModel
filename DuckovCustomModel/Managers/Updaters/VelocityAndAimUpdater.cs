using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class VelocityAndAimUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;

            var velocity = ctx.CharacterMainControl.Velocity;
            customControl.SetParameterFloat(CustomAnimatorHash.VelocityMagnitude, velocity.magnitude);
            customControl.SetParameterFloat(CustomAnimatorHash.VelocityX, velocity.x);
            customControl.SetParameterFloat(CustomAnimatorHash.VelocityY, velocity.y);
            customControl.SetParameterFloat(CustomAnimatorHash.VelocityZ, velocity.z);

            var aimDir = ctx.CharacterMainControl.CurrentAimDirection;
            customControl.SetParameterFloat(CustomAnimatorHash.AimDirX, aimDir.x);
            customControl.SetParameterFloat(CustomAnimatorHash.AimDirY, aimDir.y);
            customControl.SetParameterFloat(CustomAnimatorHash.AimDirZ, aimDir.z);

            var inAds = ctx.CharacterMainControl.IsInAdsInput;
            customControl.SetParameterBool(CustomAnimatorHash.InAds, inAds);

            var adsValue = ctx.CharacterMainControl.AdsValue;
            customControl.SetParameterFloat(CustomAnimatorHash.AdsValue, adsValue);

            var aimType = (int)ctx.CharacterMainControl.AimType;
            customControl.SetParameterInteger(CustomAnimatorHash.AimType, aimType);
        }
    }
}
