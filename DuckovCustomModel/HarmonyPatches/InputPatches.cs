using System.Reflection;
using DuckovCustomModel.Managers;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class InputPatches
    {
        [HarmonyPatch]
        internal static class InputGetKeyDownPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), [typeof(KeyCode)]);
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(KeyCode key, ref bool __result)
            {
                if (!InputBlocker.IsInputBlocked || InputBlocker.IsGettingRealInput)
                    return true;

                __result = false;
                return false;
            }
        }

        [HarmonyPatch]
        internal static class InputGetKeyPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Input), nameof(Input.GetKey), [typeof(KeyCode)]);
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(KeyCode key, ref bool __result)
            {
                if (!InputBlocker.IsInputBlocked || InputBlocker.IsGettingRealInput)
                    return true;

                __result = false;
                return false;
            }
        }

        [HarmonyPatch]
        internal static class InputGetKeyUpPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Input), nameof(Input.GetKeyUp), [typeof(KeyCode)]);
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(KeyCode key, ref bool __result)
            {
                if (!InputBlocker.IsInputBlocked || InputBlocker.IsGettingRealInput)
                    return true;

                __result = false;
                return false;
            }
        }
    }
}
