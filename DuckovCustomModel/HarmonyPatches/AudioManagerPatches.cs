using System.Reflection;
using Duckov;
using DuckovCustomModel.MonoBehaviours;
using FMOD.Studio;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class AudioManagerPatches
    {
        [HarmonyPatch]
        internal static class AudioManagerPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(AudioManager), "PostQuak");
            }

            private static bool Prefix(
                string soundKey,
                AudioManager.VoiceType voiceType,
                GameObject gameObject,
                // ReSharper disable once InconsistentNaming
                ref EventInstance? __result)
            {
                if (gameObject == null) return true;

                var characterMainControl = gameObject.GetComponent<CharacterMainControl>();
                if (characterMainControl == null) return true;

                var modelHandler = characterMainControl.GetComponent<ModelHandler>();
                if (modelHandler == null || !modelHandler.IsInitialized) return true;

                var normalizedSoundKey = string.IsNullOrWhiteSpace(soundKey)
                    ? "normal"
                    : soundKey.ToLowerInvariant().Trim();

                var soundPath = modelHandler.GetRandomSoundByTag(normalizedSoundKey);
                if (string.IsNullOrEmpty(soundPath)) return true;

                AudioManager.PostCustomSFX(soundPath);
                __result = null;
                return false;
            }
        }
    }
}