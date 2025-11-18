using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public static class SoundTags
    {
        public const string Normal = "normal";
        public const string Surprise = "surprise";
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
        public const string FootStepOrganicWalkLight = "footstep_organic_walk_light";
        public const string FootStepOrganicWalkHeavy = "footstep_organic_walk_heavy";
        public const string FootStepOrganicRunLight = "footstep_organic_run_light";
        public const string FootStepOrganicRunHeavy = "footstep_organic_run_heavy";
        public const string FootStepMechWalkLight = "footstep_mech_walk_light";
        public const string FootStepMechWalkHeavy = "footstep_mech_walk_heavy";
        public const string FootStepMechRunLight = "footstep_mech_run_light";
        public const string FootStepMechRunHeavy = "footstep_mech_run_heavy";
        public const string FootStepDangerWalkLight = "footstep_danger_walk_light";
        public const string FootStepDangerWalkHeavy = "footstep_danger_walk_heavy";
        public const string FootStepDangerRunLight = "footstep_danger_run_light";
        public const string FootStepDangerRunHeavy = "footstep_danger_run_heavy";
        public const string FootStepNoSoundWalkLight = "footstep_nosound_walk_light";
        public const string FootStepNoSoundWalkHeavy = "footstep_nosound_walk_heavy";
        public const string FootStepNoSoundRunLight = "footstep_nosound_run_light";
        public const string FootStepNoSoundRunHeavy = "footstep_nosound_run_heavy";


        public static IReadOnlyCollection<string> ValidTags =>
        [
            Normal,
            Surprise,
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
            FootStepOrganicWalkLight,
            FootStepOrganicWalkHeavy,
            FootStepOrganicRunLight,
            FootStepOrganicRunHeavy,
            FootStepMechWalkLight,
            FootStepMechWalkHeavy,
            FootStepMechRunLight,
            FootStepMechRunHeavy,
            FootStepDangerWalkLight,
            FootStepDangerWalkHeavy,
            FootStepDangerRunLight,
            FootStepDangerRunHeavy,
            FootStepNoSoundWalkLight,
            FootStepNoSoundWalkHeavy,
            FootStepNoSoundRunLight,
            FootStepNoSoundRunHeavy,
        ];
    }
}
