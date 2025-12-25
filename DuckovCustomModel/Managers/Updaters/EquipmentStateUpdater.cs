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
                if (control.ModelHandler.Target == ModelTarget.AICharacter)
                {
                    var nameKey = control.CharacterMainControl.characterPreset?.nameKey;
                    if (!string.IsNullOrEmpty(nameKey))
                        hideOriginalEquipment = ModEntry.HideEquipmentConfig
                            .GetHideAICharacterEquipment(nameKey);
                }
                else
                {
                    hideOriginalEquipment =
                        ModEntry.HideEquipmentConfig.GetHideEquipment(control.ModelHandler.Target);
                }
            }

            control.SetParameterBool(CustomAnimatorHash.HideOriginalEquipment, hideOriginalEquipment);

            var popTextSocket = CharacterModelSocketUtils.GetPopTextSocket(control.CharacterModel);
            var havePopText = popTextSocket != null && popTextSocket.childCount > 0;
            control.SetParameterBool(CustomAnimatorHash.HavePopText, havePopText);
        }
    }
}
