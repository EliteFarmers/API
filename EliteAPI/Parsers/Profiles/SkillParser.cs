using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Profiles;

public static class SkillParser
{
    public static void ParseSkills(this ProfileMember member, RawMemberData memberData)
    {
        var skills = member.Skills;

        skills.Combat = memberData.ExperienceSkillCombat ?? skills.Combat;
        skills.Mining = memberData.ExperienceSkillMining ?? skills.Mining;
        skills.Foraging = memberData.ExperienceSkillForaging ?? skills.Foraging;
        skills.Fishing = memberData.ExperienceSkillFishing ?? skills.Fishing;
        skills.Enchanting = memberData.ExperienceSkillEnchanting ?? skills.Enchanting;
        skills.Alchemy = memberData.ExperienceSkillAlchemy ?? skills.Alchemy;
        skills.Taming = memberData.ExperienceSkillTaming ?? skills.Taming;
        skills.Carpentry = memberData.ExperienceSkillCarpentry ?? skills.Carpentry;
        skills.Runecrafting = memberData.ExperienceSkillRunecrafting ?? skills.Runecrafting;
        skills.Social = memberData.ExperienceSkillSocial ?? skills.Social;
        skills.Farming = memberData.ExperienceSkillFarming ?? skills.Farming;
    }
}

