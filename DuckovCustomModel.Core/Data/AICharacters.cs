using System.Collections.Generic;

namespace DuckovCustomModel.Core.Data
{
    public static class AICharacters
    {
        public const string AllAICharactersKey = "*";

        private static readonly HashSet<string> SupportedAICharacterHashSet =
        [
            "Character_Bob",
            "Character_DemoTrophy",
            "Character_Heart7",
            "Character_Jeff",
            "Character_Ming",
            "Character_Mud",
            "Character_Orange",
            "Character_SnowPMC",
            "Character_Xavier",
            "Cname_3Shot_Child",
            "Cname_BALeader",
            "Cname_BALeader_Child",
            "Cname_Bear",
            "Cname_Boss_3Shot",
            "Cname_Boss_Arcade",
            "Cname_Boss_Fly",
            "Cname_Boss_Fly_Child",
            "Cname_Boss_Red",
            "Cname_Boss_Shot",
            "Cname_Boss_Sniper",
            "Cname_CrazyRob",
            "Cname_DengWolf",
            "Cname_Drone",
            "Cname_Football_1",
            "Cname_Football_2",
            "Cname_Ghost",
            "Cname_Grenade",
            "Cname_Hunter",
            "Cname_LabTestObjective",
            "Cname_Merchant_Myst",
            "Cname_MonsterClimb",
            "Cname_Mushroom",
            "Cname_Prison",
            "Cname_Prison_Boss",
            "Cname_Raider",
            "Cname_RobSpider",
            "Cname_Roadblock",
            "Cname_RPG",
            "Cname_Scav",
            "Cname_Scav_Ice",
            "Cname_ScavRage",
            "Cname_SchoolBully",
            "Cname_SchoolBully_Child",
            "Cname_SenorEngineer",
            "Cname_ServerGuardian",
            "Cname_ShortEagle",
            "Cname_Snow_BigIce",
            "Cname_Snow_Fleeze",
            "Cname_Snow_Igny",
            "Cname_Speedy",
            "Cname_SpeedyChild",
            "Cname_Speedy_Ice",
            "Cname_SpeedyChild_Ice",
            "Cname_StormBoss1",
            "Cname_StormBoss1_Child",
            "Cname_StormBoss2",
            "Cname_StormBoss3",
            "Cname_StormBoss4",
            "Cname_StormBoss5",
            "Cname_StormCreature",
            "Cname_StormVirus",
            "Cname_UltraMan",
            "Cname_Usec",
            "Cname_Usec_Ice",
            "Cname_Vida",
            "Cname_Wolf",
            "Cname_Wolf_Ice",
            "Cname_WolfKing_Ice",
            "Cname_XING",
            "Cname_XINGS",
        ];

        public static IReadOnlyCollection<string> SupportedAICharacters => SupportedAICharacterHashSet;

        public static void AddAICharacter(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey)) return;
            if (nameKey == AllAICharactersKey) return;
            SupportedAICharacterHashSet.Add(nameKey);
        }

        public static void AddAICharacters(params IEnumerable<string>? nameKeys)
        {
            if (nameKeys == null) return;
            foreach (var nameKey in nameKeys)
                AddAICharacter(nameKey);
        }

        public static bool Contains(string nameKey)
        {
            return SupportedAICharacterHashSet.Contains(nameKey);
        }
    }
}
