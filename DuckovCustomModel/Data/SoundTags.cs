using System.Collections.Generic;

namespace DuckovCustomModel.Data
{
    public static class SoundTags
    {
        public const string Normal = "normal";
        public const string Surprise = "surprise";
        public const string Death = "death";
        public const string Idle = "idle";

        public static IReadOnlyCollection<string> ValidTags =>
        [
            Normal,
            Surprise,
            Death,
            Idle,
        ];
    }
}