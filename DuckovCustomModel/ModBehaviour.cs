using System;
using System.Reflection;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Managers;
using HarmonyLib;

namespace DuckovCustomModel
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private Harmony? _harmony;

        public UIConfig? UIConfig;

        private void Awake()
        {
            ModLogger.Log($"{Constant.ModName} 已加载");
        }

        private void OnEnable()
        {
            LoadConfig();

            var patched = PatchAll();
            if (!patched) ModLogger.LogError("未能应用Harmony补丁，模组功能可能受到影响。");

            LevelManager.OnAfterLevelInitialized += LevelManager_OnAfterLevelInitialized;

            ModelManager.UpdateModelBundles();
        }

        private void OnDisable()
        {
            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched) ModLogger.LogError("未能删除Harmony补丁，模块卸载可能会受损。");

            LevelManager.OnAfterLevelInitialized -= LevelManager_OnAfterLevelInitialized;
        }

        private void OnDestroy()
        {
            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched)
                ModLogger.LogError("销毁时删除Harmony补丁失败，模块卸载可能会受损。");
        }

        private bool PatchAll()
        {
            try
            {
                _harmony = new(Constant.HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModLogger.Log("成功应用Harmony补丁");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"无法应用Harmony补丁: {ex}");
                return false;
            }
        }

        private bool UnpatchAll()
        {
            try
            {
                if (_harmony == null) return true;
                _harmony.UnpatchAll(_harmony.Id);
                _harmony = null;
                ModLogger.Log("Harmony补丁已成功删除");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"删除Harmony补丁时出错: {ex}");
                return false;
            }
        }

        private void LoadConfig()
        {
            UIConfig = ConfigManager.LoadConfigFromFile<UIConfig>("UIConfig.json");
            if (UIConfig.Validate()) UIConfig.SaveToFile("UIConfig.json");
        }

        private void UnloadConfig()
        {
            UIConfig = null;
        }

        private static void LevelManager_OnAfterLevelInitialized()
        {
            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            if (mainCharacterControl == null)
            {
                ModLogger.LogError("无法初始化模型管理器: MainCharacter 为 null");
                return;
            }

            ModelManager.InitializeModelHandler(mainCharacterControl);
        }
    }
}