using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Utils;

namespace DuckovCustomModel.Managers.Updaters
{
    public class EquipmentStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (!control.Initialized || control.CharacterMainControl == null || control.CharacterModel == null)
                return;

            var thermalOn = control.CharacterMainControl.ThermalOn;
            control.SetParameterBool(CustomAnimatorHash.ThermalOn, thermalOn);

            var hideOriginalEquipment = false;
            if (ModEntry.HideEquipmentConfig != null && control.ModelHandler != null)
            {
                var targetTypeId = control.ModelHandler.GetTargetTypeId();
                if (ModelTargetType.IsAICharacterTargetType(targetTypeId))
                {
                    var nameKey = control.CharacterMainControl.characterPreset?.nameKey;
                    if (!string.IsNullOrEmpty(nameKey))
                    {
                        var effectiveTargetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
                        hideOriginalEquipment = ModEntry.HideEquipmentConfig.GetHideEquipment(effectiveTargetTypeId) ||
                                                ModEntry.HideEquipmentConfig.GetHideEquipment(
                                                    ModelTargetType.AllAICharacters);
                    }
                    else
                    {
                        hideOriginalEquipment = ModEntry.HideEquipmentConfig.GetHideEquipment(
                            ModelTargetType.AllAICharacters);
                    }
                }
                else
                {
                    hideOriginalEquipment = ModEntry.HideEquipmentConfig.GetHideEquipment(targetTypeId);
                }
            }

            control.SetParameterBool(CustomAnimatorHash.HideOriginalEquipment, hideOriginalEquipment);

            var popTextSocket = CharacterModelSocketUtils.GetPopTextSocket(control.CharacterModel);
            var havePopText = popTextSocket != null && popTextSocket.childCount > 0;
            control.SetParameterBool(CustomAnimatorHash.HavePopText, havePopText);
        }
    }
}
