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

                var soundPath = modelHandler.GetRandomSoundByTag(normalizedSoundKey);
                if (string.IsNullOrEmpty(soundPath)) return true;

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

                var soundTag = character.footStepMaterialType switch
                {
                    AudioManager.FootStepMaterialType.organic => type switch
                    {
                        CharacterSoundMaker.FootStepTypes.walkLight => SoundTags.FootStepOrganicWalkLight,
                        CharacterSoundMaker.FootStepTypes.walkHeavy => SoundTags.FootStepOrganicWalkHeavy,
                        CharacterSoundMaker.FootStepTypes.runLight => SoundTags.FootStepOrganicRunLight,
                        CharacterSoundMaker.FootStepTypes.runHeavy => SoundTags.FootStepOrganicRunHeavy,
                        _ => SoundTags.FootStepOrganicWalkLight,
                    },
                    AudioManager.FootStepMaterialType.mech => type switch
                    {
                        CharacterSoundMaker.FootStepTypes.walkLight => SoundTags.FootStepMechWalkLight,
                        CharacterSoundMaker.FootStepTypes.walkHeavy => SoundTags.FootStepMechWalkHeavy,
                        CharacterSoundMaker.FootStepTypes.runLight => SoundTags.FootStepMechRunLight,
                        CharacterSoundMaker.FootStepTypes.runHeavy => SoundTags.FootStepMechRunHeavy,
                        _ => SoundTags.FootStepMechWalkLight,
                    },
                    AudioManager.FootStepMaterialType.danger => type switch
                    {
                        CharacterSoundMaker.FootStepTypes.walkLight => SoundTags.FootStepDangerWalkLight,
                        CharacterSoundMaker.FootStepTypes.walkHeavy => SoundTags.FootStepDangerWalkHeavy,
                        CharacterSoundMaker.FootStepTypes.runLight => SoundTags.FootStepDangerRunLight,
                        CharacterSoundMaker.FootStepTypes.runHeavy => SoundTags.FootStepDangerRunHeavy,
                        _ => SoundTags.FootStepDangerWalkLight,
                    },
                    _ => type switch
                    {
                        CharacterSoundMaker.FootStepTypes.walkLight => SoundTags.FootStepNoSoundWalkLight,
                        CharacterSoundMaker.FootStepTypes.walkHeavy => SoundTags.FootStepNoSoundWalkHeavy,
                        CharacterSoundMaker.FootStepTypes.runLight => SoundTags.FootStepNoSoundRunLight,
                        CharacterSoundMaker.FootStepTypes.runHeavy => SoundTags.FootStepNoSoundRunHeavy,
                        _ => SoundTags.FootStepNoSoundWalkLight,
                    },
                };

                var soundPath = modelHandler.GetRandomSoundByTag(soundTag);
                if (string.IsNullOrEmpty(soundPath)) return true;

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

                var soundPath = modelHandler.GetRandomSoundByTag(SoundTags.Normal);
                if (string.IsNullOrEmpty(soundPath)) return true;

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
    }
}
