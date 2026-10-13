using System;

namespace DuckovCustomModel.Core.Data
{
    public sealed class ModelIkSettings
    {
        public bool Enabled { get; set; } = true;
        public float Weight { get; set; } = 1;
        public float BlendSpeed { get; set; } = 12;
        public bool RightHand { get; set; } = true;
        public bool LeftHand { get; set; } = true;
        public ModelArmIkSettings RightArm { get; set; } = new();
        public ModelArmIkSettings LeftArm { get; set; } = new();
        public float[] RightHandOffset { get; set; } = [-0.1f, -0.25f, 0.6f];
        public string LeftHandSocket { get; set; } = "LeftHand";
        public float[] LeftHandOffset { get; set; } = [0, 0, 0];
        public float[] LeftHandRotation { get; set; } = [0, 0, 0];

        internal bool Validate()
        {
            return Finite(Weight) && Weight >= 0 && Weight <= 1 && Finite(BlendSpeed) && BlendSpeed > 0 &&
                   RightArm != null && RightArm.Validate() && LeftArm != null && LeftArm.Validate() &&
                   ValidVector(RightHandOffset) && ValidVector(LeftHandOffset) && ValidVector(LeftHandRotation) &&
                   !string.IsNullOrWhiteSpace(LeftHandSocket) && ValidPath(LeftHandSocket);
        }

        private static bool ValidVector(float[]? values)
        {
            return values is { Length: 3 } && Array.TrueForAll(values, Finite);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static bool ValidPath(string? path)
        {
            if (path == null) return false;
            if (path.Length == 0) return true;
            foreach (var segment in path.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == "..") return false;
                foreach (var character in segment)
                    if (char.IsControl(character) || character == '\\' || character == ':')
                        return false;
            }

            return true;
        }
    }

    public sealed class ModelArmIkSettings
    {
        public string UpperArmPath { get; set; } = string.Empty;
        public string LowerArmPath { get; set; } = string.Empty;
        public string HandPath { get; set; } = string.Empty;

        internal bool Validate()
        {
            return ModelIkSettings.ValidPath(UpperArmPath) && ModelIkSettings.ValidPath(LowerArmPath) &&
                   ModelIkSettings.ValidPath(HandPath);
        }
    }
}
