using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers.Updaters
{
    public class EquipmentTypeIDUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null || control.CharacterModel == null)
                return;

            var characterItemSlots = control.CharacterMainControl.CharacterItem.Slots;
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

            control.SetParameterInteger(CustomAnimatorHash.ArmorTypeID, armorTypeID);
            control.SetParameterBool(CustomAnimatorHash.ArmorEquip, armorTypeID > 0);

            control.SetParameterInteger(CustomAnimatorHash.HelmetTypeID, helmetTypeID);
            control.SetParameterBool(CustomAnimatorHash.HelmetEquip, helmetTypeID > 0);

            control.SetParameterInteger(CustomAnimatorHash.FaceTypeID, faceTypeID);
            control.SetParameterBool(CustomAnimatorHash.FaceEquip, faceTypeID > 0);

            control.SetParameterInteger(CustomAnimatorHash.BackpackTypeID, backpackTypeID);
            control.SetParameterBool(CustomAnimatorHash.BackpackEquip, backpackTypeID > 0);

            control.SetParameterInteger(CustomAnimatorHash.HeadsetTypeID, headsetTypeID);
            control.SetParameterBool(CustomAnimatorHash.HeadsetEquip, headsetTypeID > 0);
        }
    }
}
