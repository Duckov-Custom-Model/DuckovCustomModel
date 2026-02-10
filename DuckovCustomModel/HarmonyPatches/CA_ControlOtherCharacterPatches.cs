using DuckovCustomModel.MonoBehaviours;
using HarmonyLib;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    // ReSharper disable once InconsistentNaming
    internal class CA_ControlOtherCharacterPatches
    {
        //protected override bool OnStart()
        [HarmonyPatch(typeof(CA_ControlOtherCharacter), "OnStart")]
        [HarmonyPostfix]
        // ReSharper disable InconsistentNaming
        private static void CA_ControlOtherCharacter_OnStart_Postfix(
                CA_ControlOtherCharacter __instance,
                ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (!__result)
                return;

            var targetCharacter = __instance.targetCharacter;
            if (targetCharacter == null || !targetCharacter.isVehicle)
                return;

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null)
                return;

            modelHandler.RegisterRider(__instance.characterController);
        }

        [HarmonyPatch(typeof(CA_ControlOtherCharacter), "OnStop")]
        [HarmonyPrefix]
        // ReSharper disable InconsistentNaming
        private static void CA_ControlOtherCharacter_OnStop_Prefix(
                CA_ControlOtherCharacter __instance)
            // ReSharper restore InconsistentNaming
        {
            var targetCharacter = __instance.targetCharacter;
            if (targetCharacter == null || !targetCharacter.isVehicle)
                return;

            var modelHandler = targetCharacter.GetComponent<ModelHandler>();
            if (modelHandler == null)
                return;

            modelHandler.UnregisterRider(__instance.characterController);
        }
    }
}
