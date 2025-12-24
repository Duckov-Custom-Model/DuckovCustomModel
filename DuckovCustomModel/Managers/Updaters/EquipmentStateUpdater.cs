using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Core.Managers;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Utils;

namespace DuckovCustomModel.Managers.Updaters
{
    public class EquipmentStateUpdater : IAnimatorParameterUpdater
    {
        public void UpdateParameters(object control, object context)
        {
            if (control is not CustomAnimatorControl customControl) return;
            if (context is not AnimatorUpdateContext ctx) return;

            if (!ctx.Initialized || ctx.CharacterMainControl == null || ctx.CharacterModel == null)
                return;

            var thermalOn = ctx.CharacterMainControl.ThermalOn;
            customControl.SetParameterBool(CustomAnimatorHash.ThermalOn, thermalOn);

            var hideOriginalEquipment = false;
            if (ModEntry.HideEquipmentConfig != null && ctx.ModelHandler != null)
            {
                if (ctx.ModelHandler.Target == ModelTarget.AICharacter)
                {
                    var nameKey = ctx.CharacterMainControl?.characterPreset?.nameKey;
                    if (!string.IsNullOrEmpty(nameKey))
                        hideOriginalEquipment = ModEntry.HideEquipmentConfig
                            .GetHideAICharacterEquipment(nameKey);
                }
                else
                {
                    hideOriginalEquipment =
                        ModEntry.HideEquipmentConfig.GetHideEquipment(ctx.ModelHandler.Target);
                }
            }

            customControl.SetParameterBool(CustomAnimatorHash.HideOriginalEquipment, hideOriginalEquipment);

            var popTextSocket = CharacterModelSocketUtils.GetPopTextSocket(ctx.CharacterModel);
            var havePopText = popTextSocket != null && popTextSocket.childCount > 0;
            customControl.SetParameterBool(CustomAnimatorHash.HavePopText, havePopText);
        }
    }
}
