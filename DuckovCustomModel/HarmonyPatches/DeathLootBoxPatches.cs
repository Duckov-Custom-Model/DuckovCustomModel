using System.Collections.Generic;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Integrations.Ysm;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class DeathLootBoxPatches
    {
        private static readonly Dictionary<Item, CharacterMainControl> DeathLootBoxOwners = [];

        [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static void CharacterMainControl_OnDead_Prefix(CharacterMainControl __instance)
        {
            if (__instance == null)
                return;

            var characterItem = __instance.CharacterItem;
            if (characterItem == null)
                return;

            DeathLootBoxOwners[characterItem] = __instance;
        }

        [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void CharacterMainControl_OnDead_Postfix(CharacterMainControl __instance)
        {
            if (__instance == null)
                return;

            var characterItem = __instance.CharacterItem;
            if (characterItem == null)
                return;

            DeathLootBoxOwners.Remove(characterItem);
        }

        [HarmonyPatch(typeof(InteractableLootbox), nameof(InteractableLootbox.CreateFromItem))]
        [HarmonyPostfix]
        // ReSharper disable InconsistentNaming
        private static void InteractableLootBox_CreateFromItem_Postfix(InteractableLootbox __result, Item item)
        {
            if (__result == null || item == null)
                return;

            if (!DeathLootBoxOwners.TryGetValue(item, out var owner))
                return;

            if (owner == null)
            {
                DeathLootBoxOwners.Remove(item);
                return;
            }

            var modelRoot = __result.transform.Find("Model");
            if (modelRoot == null)
                return;

            var modelHandler = owner.GetComponent<ModelHandler>();
            if (modelHandler == null)
                return;

            if (!modelHandler.HaveCustomDeathLootBox())
                return;

            GameObject? customModel;
            try
            {
                customModel = modelHandler.CreateCustomDeathLootBoxInstance();
            }
            catch (System.Exception exception)
            {
                ModLogger.LogWarning($"Death loot box visual could not load: {exception.Message}");
                return;
            }
            if (customModel == null)
                return;
            var originals = new List<GameObject>();
            var originalActive = new List<bool>();
            foreach (Transform child in modelRoot)
            {
                originals.Add(child.gameObject);
                originalActive.Add(child.gameObject.activeSelf);
            }
            try
            {
                customModel.SetActive(false);
                customModel.transform.SetParent(modelRoot, false);
                customModel.transform.localPosition = Vector3.zero;
                customModel.transform.localRotation = Quaternion.identity;
                var ysmVisual = customModel.GetComponent<YsmDeathLootBoxVisual>();
                if (ysmVisual == null)
                    customModel.transform.localScale = Vector3.one;
                else
                    ysmVisual.MatchLayer(modelRoot.gameObject.layer);

                var destroyAdapter = customModel.GetComponent<OnDestroyAdapter>();
                if (destroyAdapter == null)
                    destroyAdapter = customModel.AddComponent<OnDestroyAdapter>();
                destroyAdapter.OnDestroyEvent += _ =>
                {
                    for (var index = 0; index < originals.Count; index++)
                        if (originals[index] != null)
                            originals[index].SetActive(originalActive[index]);
                };

                foreach (var original in originals) original.SetActive(false);
                customModel.SetActive(true);
            }
            catch (System.Exception exception)
            {
                for (var index = 0; index < originals.Count; index++)
                    if (originals[index] != null)
                        originals[index].SetActive(originalActive[index]);
                UnityEngine.Object.Destroy(customModel);
                ModLogger.LogWarning($"Death loot box visual could not attach: {exception.Message}");
            }
        }
    }
}
