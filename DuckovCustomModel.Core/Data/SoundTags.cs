using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public static class SoundTags
    {
        public const string Normal = "normal";
        public const string Surprise = "surprise";
        public const string Death = "death";
        public const string Idle = "idle";
        public const string TriggerOnHurt = "trigger_on_hurt";
        public const string TriggerOnDeath = "trigger_on_death";
        public const string SearchFoundItemQualityNone = "search_found_item_quality_none";
        public const string SearchFoundItemQualityWhite = "search_found_item_quality_white";
        public const string SearchFoundItemQualityGreen = "search_found_item_quality_green";
        public const string SearchFoundItemQualityBlue = "search_found_item_quality_blue";
        public const string SearchFoundItemQualityPurple = "search_found_item_quality_purple";
        public const string SearchFoundItemQualityOrange = "search_found_item_quality_orange";
        public const string SearchFoundItemQualityRed = "search_found_item_quality_red";
        public const string SearchFoundItemQualityQ7 = "search_found_item_quality_q7";
        public const string SearchFoundItemQualityQ8 = "search_found_item_quality_q8";

        public static IReadOnlyCollection<string> ValidTags =>
        [
            Normal,
            Surprise,
            Death,
            Idle,
            TriggerOnHurt,
            TriggerOnDeath,
            SearchFoundItemQualityNone,
            SearchFoundItemQualityWhite,
            SearchFoundItemQualityGreen,
            SearchFoundItemQualityBlue,
            SearchFoundItemQualityPurple,
            SearchFoundItemQualityOrange,
            SearchFoundItemQualityRed,
            SearchFoundItemQualityQ7,
            SearchFoundItemQualityQ8,
        ];
    }
}
