using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Tests.ParserTests;

public class SkillLevelTests
{
	[Fact]
	public void SkillLevelTest() {
		var skills = new Skills() {
			Farming = 316089009.24856186,
			Mining = 344590585.8221882,
			Combat = 26015037.770157248,
			Foraging = 4124790.1327327075,
			Fishing = 7191379.396916075,
			Enchanting = 174991298.48197016,
			Alchemy = 158150696.05243564,
			Carpentry = 198619527.16241342,
			Runecrafting = 153076.14649999992,
			Taming = 361707098.3819422,
			Social = 28743.22999999981,
		};
		
		skills.GetSkillLevel(SkillName.Farming).ShouldBe(50);
		skills.GetSkillLevel(SkillName.Mining).ShouldBe(60);
		skills.GetSkillLevel(SkillName.Combat).ShouldBe(40);
		skills.GetSkillLevel(SkillName.Foraging).ShouldBe(26);
		skills.GetSkillLevel(SkillName.Fishing).ShouldBe(29);
		skills.GetSkillLevel(SkillName.Enchanting).ShouldBe(60);
		skills.GetSkillLevel(SkillName.Alchemy).ShouldBe(50);
		skills.GetSkillLevel(SkillName.Carpentry).ShouldBe(50);
		skills.GetSkillLevel(SkillName.Runecrafting).ShouldBe(25);
		skills.GetSkillLevel(SkillName.Taming).ShouldBe(50);
		skills.GetSkillLevel(SkillName.Social).ShouldBe(15);

		skills.LevelCaps = new Dictionary<string, int>() {
			{ SkillName.Farming, 10 },
			{ SkillName.Taming, 10 },
		};
		
		skills.GetSkillLevel(SkillName.Farming).ShouldBe(60);
		skills.GetSkillLevel(SkillName.Taming).ShouldBe(60);
		
		skills.LevelCaps = new Dictionary<string, int>() {
			{ SkillName.Farming, 10 },
			{ SkillName.Taming, 1 },
		};
		
		skills.GetSkillLevel(SkillName.Taming).ShouldBe(51);
		
		skills.GetAverageSkillLevel().ShouldBe(47.3333, 0.001);
	}

	[Fact]
	public void NetworkLevelTest() {
		var playerData = new PlayerData {
			NetworkExp = 39194778,
			Uuid = "some-uuid"
		};
		
		playerData.GetNetworkLevel().ShouldBe(174);
		
		SkillParser.GetNetworkLevel(0).ShouldBe(1);
		SkillParser.GetNetworkLevel(int.MaxValue).ShouldBe(1000);
	}
}