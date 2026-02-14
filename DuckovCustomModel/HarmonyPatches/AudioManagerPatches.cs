using System.Reflection;
using Duckov;
using DuckovCustomModel.Core.Data;
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
        internal static class AudioManagerPostQuarkPatch
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

                if (!modelHandler.HasAnySounds()) return true;
                if (!modelHandler.IsModelAudioEnabled) return true;

                var normalizedSoundKey = string.IsNullOrWhiteSpace(soundKey)
                    ? SoundTags.Normal
                    : soundKey.ToLowerInvariant().Trim();

                var soundPath = modelHandler.GetRandomSoundByTag(normalizedSoundKey, out var skippedByProbability);
                if (string.IsNullOrEmpty(soundPath)) return true;

                if (skippedByProbability) return false;

                modelHandler.PlaySound("quack", soundPath, playMode: SoundPlayMode.StopPrevious);
                __result = null;
                return false;
            }
        }

        [HarmonyPatch]
        internal static class AudioManagerOnFootStepSoundPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(AudioManager), "OnFootStepSound");
            }

            // ReSharper disable InconsistentNaming
            private static bool Prefix(
                    AudioManager __instance,
                    Vector3 position,
                    CharacterSoundMaker.FootStepTypes type,
                    CharacterMainControl character)
                // ReSharper restore InconsistentNaming
            {
                if (character == null) return true;

                __instance.MSetParameter(character.gameObject, "terrain", "floor");

                var modelHandler = character.GetComponent<ModelHandler>();
                if (modelHandler == null || !modelHandler.IsInitialized) return true;

                if (!modelHandler.HasAnySounds()) return true;
                if (!modelHandler.IsModelAudioEnabled) return true;

                var typeTag = type switch
                {
                    CharacterSoundMaker.FootStepTypes.walkLight => "walk_light",
                    CharacterSoundMaker.FootStepTypes.walkHeavy => "walk_heavy",
                    CharacterSoundMaker.FootStepTypes.runLight => "run_light",
                    CharacterSoundMaker.FootStepTypes.runHeavy => "run_heavy",
                    _ => "walk_light",
                };
                var soundTag = string.Format(SoundTags.FootStepFormat,
                    character.footStepMaterialType.ToString().ToLowerInvariant(), typeTag);
                var soundPath = modelHandler.GetRandomSoundByTag(soundTag, out var skippedByProbability);
                if (string.IsNullOrEmpty(soundPath)) return true;

                if (skippedByProbability) return false;

                modelHandler.PlaySound("footstep", soundPath, playMode: SoundPlayMode.StopPrevious);

                return false;
            }
        }

        [HarmonyPatch]
        internal static class CharacterMainControlQuarkPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(CharacterMainControl), nameof(CharacterMainControl.Quack));
            }

            // ReSharper disable once InconsistentNaming
            private static bool Prefix(CharacterMainControl __instance)
            {
                var modelHandler = __instance.GetComponent<ModelHandler>();
                if (modelHandler == null || !modelHandler.IsInitialized) return true;

                if (!modelHandler.HasAnySounds()) return true;
                if (!modelHandler.IsModelAudioEnabled) return true;

                var soundPath = modelHandler.GetRandomSoundByTag(SoundTags.Normal, out var skippedByProbability);
                if (string.IsNullOrEmpty(soundPath)) return true;

                if (skippedByProbability) return false;

                modelHandler.PlaySound("quack", soundPath, playMode: SoundPlayMode.StopPrevious);
                AIMainBrain.MakeSound(new()
                {
                    fromCharacter = __instance,
                    fromObject = __instance.gameObject,
                    pos = __instance.transform.position,
                    fromTeam = __instance.Team,
                    soundType = SoundTypes.unknowNoise,
                    radius = 15f,
                });
                return false;
            }
        }

        [HarmonyPatch]
        internal static class AudioManagerPostEventWithGameObjectPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(AudioManager), nameof(AudioManager.Post),
                    [typeof(string), typeof(GameObject)]);
            }

            private static bool Prefix(
                string eventName,
                GameObject gameObject,
                // ReSharper disable once InconsistentNaming
                ref EventInstance? __result)
            {
                if (gameObject == null) return true;

                var characterMainControl = gameObject.GetComponentInParent<CharacterMainControl>(gameObject);
                if (characterMainControl == null) return true;

                var modelHandler = characterMainControl.GetComponent<ModelHandler>();
                if (modelHandler == null || !modelHandler.IsInitialized) return true;

                if (!modelHandler.HasAnySounds()) return true;
                if (!modelHandler.IsModelAudioEnabled) return true;

                var normalizedEventName = string.IsNullOrWhiteSpace(eventName)
                    ? SoundTags.Normal
                    : eventName.ToLowerInvariant().Trim();

                var soundPath = modelHandler.GetRandomSoundByTag(normalizedEventName, out var skippedByProbability);
                if (string.IsNullOrEmpty(soundPath)) return true;

                if (skippedByProbability) return false;

                modelHandler.PlaySound(normalizedEventName, soundPath, playMode: SoundPlayMode.StopPrevious);
                __result = null;
                return false;
            }
        }
    }
}
