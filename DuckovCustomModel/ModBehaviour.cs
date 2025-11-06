using System;
using System.Reflection;
using DuckovCustomModel.Configs;
using DuckovCustomModel.Managers;
using HarmonyLib;

namespace DuckovCustomModel
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour? Instance;
        private Harmony? _harmony;
        public UIConfig? UIConfig;
        public UsingModel? UsingModel;

        private void Awake()
        {
            if (Instance != null)
            {
                ModLogger.LogError("Multiple instances of ModBehaviour detected! There should only be one instance.");
                Destroy(this);
                return;
            }

            Instance = this;
            ModLogger.Log($"{Constant.ModName} 已加载");
        }

        private void OnEnable()
        {
            LoadConfig();

            var patched = PatchAll();
            if (!patched) ModLogger.LogError("Unable to apply Harmony patches, the mod may not function correctly.");

            LevelManager.OnAfterLevelInitialized += LevelManager_OnAfterLevelInitialized;

            ModelManager.UpdateModelBundles();
        }

        private void OnDisable()
        {
            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched) ModLogger.LogError("Unable to remove Harmony patches, the mod may not unload correctly.");

            LevelManager.OnAfterLevelInitialized -= LevelManager_OnAfterLevelInitialized;
        }

        private void OnDestroy()
        {
            UnloadConfig();

            var unpatched = UnpatchAll();
            if (!unpatched)
                ModLogger.LogError("Unable to remove Harmony patches, the mod may not unload correctly.");
        }

        private bool PatchAll()
        {
            try
            {
                _harmony = new(Constant.HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModLogger.Log("Successfully applied Harmony patches");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Unable to apply Harmony patches: {ex}");
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
                ModLogger.Log("Harmony patches removed successfully");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Unable to remove Harmony patches: {ex}");
                return false;
            }
        }

        private void LoadConfig()
        {
            UIConfig = ConfigManager.LoadConfigFromFile<UIConfig>("UIConfig.json");
            if (UIConfig.Validate()) UIConfig.SaveToFile("UIConfig.json");

            UsingModel = ConfigManager.LoadConfigFromFile<UsingModel>("UsingModel.json");
            if (UsingModel.Validate()) UsingModel.SaveToFile("UsingModel.json");

            ModLogger.Log("Configuration files loaded successfully");
        }

        private void UnloadConfig()
        {
            UIConfig = null;
            UsingModel = null;
        }

        private void LevelManager_OnAfterLevelInitialized()
        {
            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            if (mainCharacterControl == null)
            {
                ModLogger.LogError("Unable to initialize ModelManager: MainCharacterControl is null");
                return;
            }

            var modelHandler = ModelManager.InitializeModelHandler(mainCharacterControl);
            if (modelHandler == null)
            {
                ModLogger.LogError("Unable to initialize ModelManager: ModelHandler is null");
                return;
            }

            if (UsingModel == null) return;

            if (!ModelManager.FindModelByID(UsingModel.ModelID, out var bundleInfo, out var modelInfo))
                return;

            modelHandler.InitializeCustomModel(bundleInfo, modelInfo);
        }
    }
}