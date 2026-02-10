using System;
using System.Collections.Generic;
using Duckov.UI;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;
using HarmonyLib;
using ItemStatsSystem;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal class ItemDisplayPatches
    {
        private static readonly Lazy<Dictionary<DisplayQuality, string>> QualitySoundTagsLazy
            = new(() =>
            {
                var dic = new Dictionary<DisplayQuality, string>();
                foreach (DisplayQuality quality in Enum.GetValues(typeof(DisplayQuality)))
                    dic[quality] = string.Format(SoundTags.SearchFoundItemQualityFormat,
                        quality.ToString().ToLowerInvariant());
                return dic;
            });

        private static readonly HashSet<Item> RecordedItems = [];
        private static DisplayQuality? _highestPlayedQuality;
        private static IReadOnlyDictionary<DisplayQuality, string> QualitySoundTags => QualitySoundTagsLazy.Value;

        [HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.Setup))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void ItemDisplay_Setup_Postfix(ItemDisplay __instance, Item target)
        {
            if (target == null) return;
            if (RecordedItems.Contains(target)) return;
            if (!target.NeedInspection) return;

            RecordedItems.Add(target);
            target.onDestroy += OnItemDestroyed;
        }

        [HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.OnTargetInspectionStateChanged))]
        [HarmonyPostfix]
        private static void ItemDisplay_OnTargetInspectionStateChanged_Postfix(Item item)
        {
            if (item == null) return;
            if (item.Inspecting || !item.Inspected) return;
            if (!RecordedItems.Contains(item)) return;

            item.onDestroy -= OnItemDestroyed;
            RecordedItems.Remove(item);

            var mainPlayer = CharacterMainControl.Main;
            if (mainPlayer == null) return;

            var modelHandler = mainPlayer.GetComponent<ModelHandler>();
            if (modelHandler == null || !modelHandler.IsInitialized) return;
            if (!modelHandler.IsModelAudioEnabled) return;
            if (!modelHandler.HasAnySounds()) return;

            if (!QualitySoundTags.TryGetValue(item.DisplayQuality, out var soundTag)) return;

            var soundPath = modelHandler.GetRandomSoundByTag(soundTag, out var skippedByProbability);
            if (string.IsNullOrEmpty(soundPath) || skippedByProbability) return;

            const string eventName = "ItemInspectionSound";
            var isPlaying = modelHandler.IsSoundPlaying(eventName);

            if (!isPlaying) _highestPlayedQuality = null;

            var playMode = SoundPlayMode.SkipIfPlaying;
            if (_highestPlayedQuality.HasValue)
            {
                if (item.DisplayQuality > _highestPlayedQuality.Value)
                {
                    playMode = SoundPlayMode.StopPrevious;
                    _highestPlayedQuality = item.DisplayQuality;
                }
            }
            else
            {
                _highestPlayedQuality = item.DisplayQuality;
            }

            modelHandler.PlaySound(eventName, soundPath, playMode: playMode);
        }

        private static void OnItemDestroyed(Item item)
        {
            if (item == null) return;
            if (!RecordedItems.Contains(item)) return;

            RecordedItems.Remove(item);
        }
    }
}
