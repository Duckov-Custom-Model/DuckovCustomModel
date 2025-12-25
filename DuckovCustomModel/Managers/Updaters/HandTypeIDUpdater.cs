using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Utils;

namespace DuckovCustomModel.Managers.Updaters
{
    public class HandTypeIDUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null || control.CharacterModel == null)
                return;

            var leftHandTypeID = 0;
            var rightHandTypeID = 0;
            var meleeWeaponTypeID = 0;
            var weaponInLocator = 0;

            var currentHoldItemAgent = control.CharacterMainControl?.CurrentHoldItemAgent;
            if (currentHoldItemAgent != null)
                switch (currentHoldItemAgent.handheldSocket)
                {
                    case HandheldSocketTypes.leftHandSocket:
                        var leftHandSocket = CharacterModelSocketUtils.GetLeftHandSocket(control.CharacterModel);
                        if (leftHandSocket != null)
                        {
                            leftHandTypeID = currentHoldItemAgent.Item.TypeID;
                            weaponInLocator = (int)HandheldSocketTypes.leftHandSocket;
                        }
                        else
                        {
                            rightHandTypeID = currentHoldItemAgent.Item.TypeID;
                            weaponInLocator = (int)HandheldSocketTypes.normalHandheld;
                        }

                        break;
                    case HandheldSocketTypes.meleeWeapon:
                        meleeWeaponTypeID = currentHoldItemAgent.Item.TypeID;
                        weaponInLocator = (int)HandheldSocketTypes.meleeWeapon;
                        break;
                    case HandheldSocketTypes.normalHandheld:
                    default:
                        rightHandTypeID = currentHoldItemAgent.Item.TypeID;
                        weaponInLocator = (int)HandheldSocketTypes.normalHandheld;
                        break;
                }

            control.SetParameterInteger(CustomAnimatorHash.WeaponInLocator, weaponInLocator);
            control.SetParameterInteger(CustomAnimatorHash.LeftHandTypeID, leftHandTypeID);
            control.SetParameterBool(CustomAnimatorHash.LeftHandEquip, leftHandTypeID > 0);
            control.SetParameterInteger(CustomAnimatorHash.RightHandTypeID, rightHandTypeID);
            control.SetParameterBool(CustomAnimatorHash.RightHandEquip, rightHandTypeID > 0);
            control.SetParameterInteger(CustomAnimatorHash.MeleeWeaponTypeID, meleeWeaponTypeID);
            control.SetParameterBool(CustomAnimatorHash.MeleeWeaponEquip, meleeWeaponTypeID > 0);
        }
    }
}
