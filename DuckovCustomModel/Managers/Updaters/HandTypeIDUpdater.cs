using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Utils;

namespace DuckovCustomModel.Managers.Updaters
{
    public class HandTypeIDUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null || ctx.CharacterModel == null)
                return;

            var leftHandTypeID = 0;
            var rightHandTypeID = 0;
            var meleeWeaponTypeID = 0;
            var weaponInLocator = 0;

            var currentHoldItemAgent = ctx.CharacterMainControl?.CurrentHoldItemAgent;
            if (currentHoldItemAgent != null)
                switch (currentHoldItemAgent.handheldSocket)
                {
                    case HandheldSocketTypes.leftHandSocket:
                        var leftHandSocket = CharacterModelSocketUtils.GetLeftHandSocket(ctx.CharacterModel);
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

            customControl.SetParameterInteger(CustomAnimatorHash.WeaponInLocator, weaponInLocator);
            customControl.SetParameterInteger(CustomAnimatorHash.LeftHandTypeID, leftHandTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.LeftHandEquip, leftHandTypeID > 0);
            customControl.SetParameterInteger(CustomAnimatorHash.RightHandTypeID, rightHandTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.RightHandEquip, rightHandTypeID > 0);
            customControl.SetParameterInteger(CustomAnimatorHash.MeleeWeaponTypeID, meleeWeaponTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.MeleeWeaponEquip, meleeWeaponTypeID > 0);
        }
    }
}
