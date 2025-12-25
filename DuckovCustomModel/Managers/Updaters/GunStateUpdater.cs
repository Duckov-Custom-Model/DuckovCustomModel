using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class GunStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null) return;

            var gunAgent = control.GunAgent;
            if (control.HoldAgent != null && gunAgent == null)
            {
                gunAgent = control.HoldAgent as ItemAgent_Gun;
                control.SetHoldAgent(control.HoldAgent);
            }

            var isGunReady = false;
            var isReloading = false;
            var ammoRate = 0.0f;
            var shootMode = -1;
            var gunState = -1;
            if (gunAgent != null)
            {
                isReloading = gunAgent.IsReloading();
                isGunReady = gunAgent.BulletCount > 0 && !isReloading;
                shootMode = (int)gunAgent.GunItemSetting.triggerMode;
                gunState = (int)gunAgent.GunState;
                var maxAmmo = gunAgent.Capacity;
                if (maxAmmo > 0)
                    ammoRate = (float)gunAgent.BulletCount / maxAmmo;
            }

            control.SetParameterInteger(CustomAnimatorHash.GunState, gunState);
            control.SetParameterInteger(CustomAnimatorHash.ShootMode, shootMode);
            control.SetParameterFloat(CustomAnimatorHash.AmmoRate, ammoRate);
            control.SetParameterBool(CustomAnimatorHash.Reloading, isReloading);
            control.SetParameterBool(CustomAnimatorHash.GunReady, isGunReady);
        }
    }
}
