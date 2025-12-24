using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class GunStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null) return;

            if (ctx.HoldAgent != null && ctx.GunAgent == null)
                ctx.GunAgent = ctx.HoldAgent as ItemAgent_Gun;

            var isGunReady = false;
            var isReloading = false;
            var ammoRate = 0.0f;
            var shootMode = -1;
            var gunState = -1;
            if (ctx.GunAgent != null)
            {
                isReloading = ctx.GunAgent.IsReloading();
                isGunReady = ctx.GunAgent.BulletCount > 0 && !isReloading;
                shootMode = (int)ctx.GunAgent.GunItemSetting.triggerMode;
                gunState = (int)ctx.GunAgent.GunState;
                var maxAmmo = ctx.GunAgent.Capacity;
                if (maxAmmo > 0)
                    ammoRate = (float)ctx.GunAgent.BulletCount / maxAmmo;
            }

            customControl.SetParameterInteger(CustomAnimatorHash.GunState, gunState);
            customControl.SetParameterInteger(CustomAnimatorHash.ShootMode, shootMode);
            customControl.SetParameterFloat(CustomAnimatorHash.AmmoRate, ammoRate);
            customControl.SetParameterBool(CustomAnimatorHash.Reloading, isReloading);
            customControl.SetParameterBool(CustomAnimatorHash.GunReady, isGunReady);
        }
    }
}
