using System;
using System.Linq;

namespace DuckovCustomModel
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
#pragma warning disable CA2211
        public static ModBehaviour? Instance;
#pragma warning restore CA2211

        private static readonly ulong[] ValidWorkshopItemIds =
        [
            3600560151UL,
        ];

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
            if (info.isSteamItem && !IsValidWorkshopItem(info.publishedFileId))
            {
                ModLogger.LogError($"""
                                    ============================================================
                                    Error: Unrecognized Steam Workshop Item ID.
                                    The mod will not load to ensure your safety.
                                    ============================================================
                                    The Steam Workshop Item ID {info.publishedFileId} is not recognized as a valid source for {Constant.ModName}.
                                    To ensure you have the official and safe version of this mod, please download it from the official source:
                                    https://duckov-custom-model.ritsukage.com/
                                    ============================================================
                                    """);
                return;
            }

            ModLoader.Initialize();
            HarmonyLoader.Initialize();
        }

        public event Action? OnModDisabled;

        private static bool IsValidWorkshopItem(ulong publishedFileId)
        {
            return ValidWorkshopItemIds.Any(id => id == publishedFileId);
        }
    }
}
