using System;
using System.Collections.Generic;
using ModelRuntime;
using ModelRuntime.Adapters;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class DuckovYsmBindingProfile
    {
        public string ModelTarget { get; set; } = "player/main";
        public MolangValueCompatibility ValueCompatibility { get; set; } = MolangValueCompatibility.Ysm241;
        public YsmHandAnimationCompatibility HandCompatibility { get; set; } = YsmHandAnimationCompatibility.Version241;
        public string ModelID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string BundlePath { get; set; } = string.Empty;
        public string DeathLootBoxPrefabPath { get; set; } = string.Empty;
        public string DeathLootBoxYsmPath { get; set; } = string.Empty;
        public string DeathLootBoxAnimation { get; set; } = string.Empty;
        public string[] TargetTypes { get; set; } = [];
        public float Scale { get; set; } = 1f;
        public float TeleportDistance { get; set; } = 8f;
        public float AttackDurationSeconds { get; set; } = .3f;
        public float HurtDurationSeconds { get; set; } = .3f;
        public float HeadPitchLimit { get; set; } = 45f;
        public float HeadYawLimit { get; set; } = 85f;
        public float HeadPitchSign { get; set; } = 1f;
        public float HeadYawSign { get; set; } = 1f;
        public float LimbSwingScale { get; set; } = 1f;
        public Dictionary<int, string> ItemCategories { get; set; } = [];
        public Dictionary<int, string> ItemUseAnimations { get; set; } = [];
        public Dictionary<int, string[]> ItemTags { get; set; } = [];
        public Dictionary<string, string> LocatorMappings { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, DuckovYsmLocatorOffset> LocatorOffsets { get; set; } = new(StringComparer.Ordinal);
        public string FireAnimation { get; set; } = string.Empty;
        public string ReloadAnimation { get; set; } = string.Empty;
        public string DashAnimation { get; set; } = string.Empty;

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(ModelTarget)) throw new ArgumentException("ModelTarget is required.");
            if (!Enum.IsDefined(typeof(MolangValueCompatibility), ValueCompatibility)
                || !Enum.IsDefined(typeof(YsmHandAnimationCompatibility), HandCompatibility))
                throw new ArgumentException("Unsupported YSM compatibility setting.");
            Positive(Scale, nameof(Scale));
            Positive(TeleportDistance, nameof(TeleportDistance));
            Positive(AttackDurationSeconds, nameof(AttackDurationSeconds));
            Positive(HurtDurationSeconds, nameof(HurtDurationSeconds));
            Nonnegative(HeadPitchLimit, nameof(HeadPitchLimit));
            Nonnegative(HeadYawLimit, nameof(HeadYawLimit));
            Nonnegative(LimbSwingScale, nameof(LimbSwingScale));
            if (Math.Abs(HeadPitchSign) != 1f || Math.Abs(HeadYawSign) != 1f)
                throw new ArgumentException("Head angle signs must be 1 or -1.");
            ItemCategories ??= [];
            ItemUseAnimations ??= [];
            ItemTags ??= [];
            LocatorMappings ??= new(StringComparer.Ordinal);
            TargetTypes ??= [];
            LocatorOffsets ??= new(StringComparer.Ordinal);
            foreach (var pair in LocatorOffsets)
            {
                if (pair.Value == null) throw new ArgumentException("LocatorOffsets entries cannot be null.");
                ValidateLocatorVector(pair.Value.Position, pair.Key);
                ValidateLocatorVector(pair.Value.Rotation, pair.Key);
            }
        }

        private static void ValidateLocatorVector(float[] values, string socket)
        {
            if (values == null || values.Length != 3)
                throw new ArgumentException($"Locator offset for '{socket}' must contain exactly three numbers.");
            foreach (var value in values)
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentException($"Locator offset for '{socket}' must contain finite numbers.");
        }

        private static void Positive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                throw new ArgumentException(name + " must be finite and positive.");
        }

        private static void Nonnegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentException(name + " must be finite and nonnegative.");
        }
    }
}
