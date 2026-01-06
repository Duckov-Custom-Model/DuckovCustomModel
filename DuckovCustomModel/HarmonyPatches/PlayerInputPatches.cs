using System.Reflection;
using DuckovCustomModel.Managers;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class PlayerInputPatches
    {
        [HarmonyPatch]
        internal static class PlayerInputActivateInputPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(PlayerInput), nameof(PlayerInput.ActivateInput));
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(PlayerInput __instance)
            {
                if (InputBlocker.Instance == null) return true;
                if (InputBlocker.Instance.IsBlockerCalling) return true;
                if (__instance != GameManager.MainPlayerInput) return true;

                InputBlocker.Instance.IsExternalBlocking = false;

                return false;
            }
        }

        [HarmonyPatch]
        internal static class PlayerInputDeactivateInputPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(PlayerInput), nameof(PlayerInput.DeactivateInput));
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(PlayerInput __instance)
            {
                if (InputBlocker.Instance == null) return true;
                if (InputBlocker.Instance.IsBlockerCalling) return true;
                if (__instance != GameManager.MainPlayerInput) return true;

                InputBlocker.Instance.IsExternalBlocking = true;

                return false;
            }
        }
    }
}
