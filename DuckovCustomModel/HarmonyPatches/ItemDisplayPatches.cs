using System.Collections.Generic;
using Duckov;
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
        private static readonly IReadOnlyDictionary<DisplayQuality, string> QualitySoundTags =
            new Dictionary<DisplayQuality, string>
            {
                { DisplayQuality.None, SoundTags.SearchFoundItemQualityNone },
                { DisplayQuality.White, SoundTags.SearchFoundItemQualityWhite },
                { DisplayQuality.Green, SoundTags.SearchFoundItemQualityGreen },
                { DisplayQuality.Blue, SoundTags.SearchFoundItemQualityBlue },
                { DisplayQuality.Purple, SoundTags.SearchFoundItemQualityPurple },
                { DisplayQuality.Orange, SoundTags.SearchFoundItemQualityOrange },
                { DisplayQuality.Red, SoundTags.SearchFoundItemQualityRed },
                { DisplayQuality.Q7, SoundTags.SearchFoundItemQualityQ7 },
                { DisplayQuality.Q8, SoundTags.SearchFoundItemQualityQ8 },
            };

        private static readonly HashSet<Item> RecordedItems = [];

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

            var soundPath = modelHandler.GetRandomSoundByTag(soundTag);
            if (string.IsNullOrEmpty(soundPath)) return;

            AudioManager.PostCustomSFX(soundPath);
        }

        private static void OnItemDestroyed(Item item)
        {
            if (item == null) return;
            if (!RecordedItems.Contains(item)) return;

            RecordedItems.Remove(item);
        }
    }
}
