using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class EquipmentTypeIDUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null || ctx.CharacterModel == null)
                return;

            var characterItemSlots = ctx.CharacterMainControl.CharacterItem.Slots;
            var armorSlot = characterItemSlots.GetSlot(CharacterEquipmentController.armorHash);
            var helmetSlot = characterItemSlots.GetSlot(CharacterEquipmentController.helmatHash);
            var faceSlot = characterItemSlots.GetSlot(CharacterEquipmentController.faceMaskHash);
            var backpackSlot = characterItemSlots.GetSlot(CharacterEquipmentController.backpackHash);
            var headsetSlot = characterItemSlots.GetSlot(CharacterEquipmentController.headsetHash);

            var armorTypeID = armorSlot?.Content != null ? armorSlot.Content.TypeID : 0;
            var helmetTypeID = helmetSlot?.Content != null ? helmetSlot.Content.TypeID : 0;
            var faceTypeID = faceSlot?.Content != null ? faceSlot.Content.TypeID : 0;
            var backpackTypeID = backpackSlot?.Content != null ? backpackSlot.Content.TypeID : 0;
            var headsetTypeID = headsetSlot?.Content != null ? headsetSlot.Content.TypeID : 0;

            customControl.SetParameterInteger(CustomAnimatorHash.ArmorTypeID, armorTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.ArmorEquip, armorTypeID > 0);

            customControl.SetParameterInteger(CustomAnimatorHash.HelmetTypeID, helmetTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.HelmetEquip, helmetTypeID > 0);

            customControl.SetParameterInteger(CustomAnimatorHash.FaceTypeID, faceTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.FaceEquip, faceTypeID > 0);

            customControl.SetParameterInteger(CustomAnimatorHash.BackpackTypeID, backpackTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.BackpackEquip, backpackTypeID > 0);

            customControl.SetParameterInteger(CustomAnimatorHash.HeadsetTypeID, headsetTypeID);
            customControl.SetParameterBool(CustomAnimatorHash.HeadsetEquip, headsetTypeID > 0);
        }
    }
}
