using System;

namespace DuckovCustomModel
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
#pragma warning disable CA2211
        public static ModBehaviour? Instance;
#pragma warning restore CA2211

        private void Awake()
        {
            if (Instance != null)
            {
                ModLogger.LogWarning("Multiple instances of ModBehaviour detected. Destroying duplicate.");
                Destroy(this);
                return;
            }

            Instance = this;
            ModLogger.Log($"{Constant.ModName} Loaded");
        }

        private void OnDisable()
        {
            OnModDisabled?.Invoke();

            ModLoader.Uninitialize();
            HarmonyLoader.Uninitialize();
        }

        private void OnDestroy()
        {
            ModLoader.Uninitialize();
            HarmonyLoader.Uninitialize();

            Instance = null;
        }

        protected override void OnAfterSetup()
        {
            if (info.isSteamItem && info.publishedFileId != 3600560151)
            {
                ModLogger.Log($"Skipping mod loading: Steam item {info.publishedFileId} is not the target item.");
                return;
            }

            ModLoader.Initialize();
            HarmonyLoader.Initialize();
        }

        public event Action? OnModDisabled;
    }
}
