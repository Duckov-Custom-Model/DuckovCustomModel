using Duckov;
using DuckovCustomModel.Localizations;
using DuckovCustomModel.Managers;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DuckovCustomModel.HarmonyPatches
{
    [HarmonyPatch]
    internal static class GameVersionDisplayPatches
    {
        private const string UpdateColorHex = "#FF9900";
        private const string VersionObjectName = "DCMVersionDisplay";
        private static TextMeshProUGUI? _versionTextComponent;
        private static GameObject? _versionObject;

        public static void Initialize()
        {
            var gameVersionDisplay = Object.FindFirstObjectByType<GameVersionDisplay>();
            if (gameVersionDisplay == null)
                return;

            var parentTransform = gameVersionDisplay.transform.parent;
            if (parentTransform == null)
                return;

            CreateVersionObject(parentTransform, gameVersionDisplay.gameObject);
            UpdateVersionText();
        }

        [HarmonyPatch(typeof(GameVersionDisplay), "Start")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void GameVersionDisplay_Start_Postfix(GameVersionDisplay __instance)
        {
            if (__instance == null)
                return;

            if (IsVersionObjectValid())
            {
                UpdateVersionText();
                return;
            }

            var parentTransform = __instance.transform.parent;
            if (parentTransform == null)
                return;

            if (CreateVersionObject(parentTransform, __instance.gameObject))
                UpdateVersionText();
        }

        private static bool IsVersionObjectValid()
        {
            return _versionObject != null && _versionTextComponent != null;
        }

        private static bool CreateVersionObject(Transform parentTransform, GameObject templateObject)
        {
            var cloneObject = Object.Instantiate(templateObject, parentTransform);
            if (cloneObject == null)
                return false;

            cloneObject.name = VersionObjectName;

            RemoveComponent(cloneObject.GetComponent<GameVersionDisplay>());

            var textComponent = cloneObject.GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                Object.Destroy(cloneObject);
                return false;
            }

            _versionObject = cloneObject;
            _versionTextComponent = textComponent;
            textComponent.richText = true;
            textComponent.text = $"{Constant.ModName} v{Constant.ModVersion}";
            return true;
        }

        private static void UpdateVersionText()
        {
            if (_versionTextComponent == null)
                return;

            var updateChecker = UpdateChecker.Instance;
            if (updateChecker == null)
            {
                _versionTextComponent.text = $"{Constant.ModName} v{Constant.ModVersion}";
                return;
            }

            var hasUpdate = updateChecker.HasUpdate();
            var latestVersion = updateChecker.GetLatestVersion();

            _versionTextComponent.text = hasUpdate && !string.IsNullOrEmpty(latestVersion)
                ? $"{Constant.ModName} v{Constant.ModVersion} - <color={UpdateColorHex}>{Localization.UpdateAvailable} v{latestVersion}</color>"
                : $"{Constant.ModName} v{Constant.ModVersion}";
        }

        public static void RefreshUpdateVersionDisplay()
        {
            if (!IsVersionObjectValid())
                return;

            UpdateVersionText();
        }

        public static void Cleanup()
        {
            if (_versionObject != null)
            {
                Object.Destroy(_versionObject);
                _versionObject = null;
            }

            _versionTextComponent = null;
        }

        private static void RemoveComponent(Component? component)
        {
            if (component != null) Object.Destroy(component);
        }
    }
}
