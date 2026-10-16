using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.Core.Data
{
    public sealed class MotionPlaybackSettings
    {
        public string Audio { get; set; } = string.Empty;
        public bool Loop { get; set; }
        public bool InterruptOnMove { get; set; } = true;
        public bool InPlace { get; set; } = true;
        public bool UpperBodyOnly { get; set; }
        public float Speed { get; set; } = 1;
        public float Volume { get; set; } = 1;
        public bool SpatialAudio { get; set; } = true;
        public float AudioOffset { get; set; }
        public float BlendIn { get; set; } = .2f;
        public float BlendOut { get; set; } = .2f;
        public float PositionScale { get; set; } = 1;
        public float RotationY { get; set; } = 180;
        public float ArmAngle { get; set; } = 40;
        public Dictionary<string, string> BoneMappings { get; set; } = new();
        public Dictionary<string, string> ExpressionMappings { get; set; } = new();

        public bool Validate()
        {
            Audio = Audio?.Trim() ?? string.Empty;
            BoneMappings ??= new();
            ExpressionMappings ??= new();
            foreach (var pair in BoneMappings)
                if (string.IsNullOrWhiteSpace(pair.Key) || !Enum.TryParse(pair.Value, true, out HumanBodyBones bone) ||
                    !Enum.IsDefined(typeof(HumanBodyBones), bone) || bone == HumanBodyBones.LastBone)
                    return false;
            foreach (var pair in ExpressionMappings)
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    return false;
            return Finite(Speed) && Speed >= .1f && Speed <= 5 && Finite(Volume) && Volume >= 0 &&
                   Volume <= 1 && Finite(AudioOffset) && Math.Abs(AudioOffset) <= 3600 &&
                   Finite(BlendIn) && BlendIn >= 0 && BlendIn <= 10 && Finite(BlendOut) && BlendOut >= 0 &&
                   BlendOut <= 10 && Finite(PositionScale) && PositionScale > 0 && PositionScale <= 100 &&
                   Finite(RotationY) && Finite(ArmAngle) && Math.Abs(ArmAngle) <= 90;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
