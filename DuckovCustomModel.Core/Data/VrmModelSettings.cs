using System;
using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public sealed class VrmModelSettings
    {
        public string AnimatorControllerPath { get; set; } = string.Empty;
        public float Scale { get; set; } = 1;
        public float RotationY { get; set; }
        public Dictionary<string, VrmLocatorSettings> Locators { get; set; } = [];

        internal bool Validate()
        {
            if (float.IsNaN(Scale) || float.IsInfinity(Scale) || Scale <= 0 ||
                float.IsNaN(RotationY) || float.IsInfinity(RotationY)) return false;
            Locators ??= [];
            foreach (var locator in Locators.Values)
                if (locator == null || !locator.Validate())
                    return false;
            return true;
        }
    }

    public sealed class VrmLocatorSettings
    {
        public string Bone { get; set; } = string.Empty;
        public float[] Position { get; set; } = [0, 0, 0];
        public float[] Rotation { get; set; } = [0, 0, 0];

        internal bool Validate()
        {
            return !string.IsNullOrWhiteSpace(Bone) && Valid(Position) && Valid(Rotation);
        }

        private static bool Valid(float[]? values)
        {
            if (values is not { Length: 3 }) return false;
            return Array.TrueForAll(values, value => !float.IsNaN(value) && !float.IsInfinity(value));
        }
    }
}
