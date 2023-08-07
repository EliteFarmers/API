using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Mappers.Profiles;

public static class SkillParser
{
    public static void ParseSkills(this ProfileMember member, RawMemberData memberData)
    {
        var skills = member.Skills;

        member.Api.Skills =
            memberData.ExperienceSkillCombat is not null && memberData.ExperienceSkillMining is not null;

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

