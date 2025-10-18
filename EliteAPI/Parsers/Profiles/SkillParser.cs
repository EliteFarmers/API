using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Profiles;

public static class SkillParser
{
	public static void ParseSkills(this ProfileMember member, ProfileMemberResponse memberData) {
		var skills = member.Skills;
		var incoming = memberData.PlayerData?.Experience;

		if (incoming is null) {
			member.Api.Skills = false;
			return;
		}

		member.Api.Skills = incoming.SkillCombat is not null && incoming.SkillMining is not null;

		skills.Combat = incoming.SkillCombat ?? skills.Combat;
		skills.Mining = incoming.SkillMining ?? skills.Mining;
		skills.Foraging = incoming.SkillForaging ?? skills.Foraging;
		skills.Fishing = incoming.SkillFishing ?? skills.Fishing;
		skills.Enchanting = incoming.SkillEnchanting ?? skills.Enchanting;
		skills.Alchemy = incoming.SkillAlchemy ?? skills.Alchemy;
		skills.Taming = incoming.SkillTaming ?? skills.Taming;
		skills.Carpentry = incoming.SkillCarpentry ?? skills.Carpentry;
		skills.Runecrafting = incoming.SkillRunecrafting ?? skills.Runecrafting;
		skills.Social = incoming.SkillSocial ?? skills.Social;
		skills.Farming = incoming.SkillFarming ?? skills.Farming;
	}

	public static Dictionary<string, double> ExtractSkills(this SkillExperience skills) {
		return new Dictionary<string, double> {
			{ "combat", skills.Combat },
			{ "mining", skills.Mining },
			{ "foraging", skills.Foraging },
			{ "fishing", skills.Fishing },
			{ "enchanting", skills.Enchanting },
			{ "alchemy", skills.Alchemy },
			{ "taming", skills.Taming },
			{ "carpentry", skills.Carpentry },
			{ "runecrafting", skills.Runecrafting },
			{ "social", skills.Social },
			{ "farming", skills.Farming }
		};
	}
}