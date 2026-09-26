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
            var isGunReady = false;
            var isReloading = false;
            var ammoRate = 0.0f;
            var ammo = 0;
            var maxAmmo = 0;
            var shootMode = -1;
            var gunState = -1;
            if (gunAgent != null)
            {
                isReloading = gunAgent.IsReloading();
                isGunReady = gunAgent.BulletCount > 0 && !isReloading;
                shootMode = (int)gunAgent.GunItemSetting.triggerMode;
                gunState = (int)gunAgent.GunState;
                ammo = gunAgent.BulletCount;
                maxAmmo = gunAgent.Capacity;
                if (maxAmmo > 0)
                    ammoRate = (float)ammo / maxAmmo;
            }

            control.SetParameterInteger(CustomAnimatorHash.GunState, gunState);
            control.SetParameterInteger(CustomAnimatorHash.ShootMode, shootMode);
            control.SetParameterInteger(CustomAnimatorHash.Ammo, ammo);
            control.SetParameterInteger(CustomAnimatorHash.MaxAmmo, maxAmmo);
            control.SetParameterFloat(CustomAnimatorHash.AmmoRate, ammoRate);
            control.SetParameterBool(CustomAnimatorHash.Reloading, isReloading);
            control.SetParameterBool(CustomAnimatorHash.GunReady, isGunReady);
        }
    }
}
