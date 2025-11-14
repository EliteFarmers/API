using EliteFarmers.HypixelAPI.DTOs;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Profiles.Models;

[Mapper]
public static partial class SlayersMapper
{
	public static partial SlayersDto ToDto(this Slayers slayers);
	public static partial SlayerBossDto ToDto(this SlayerBossDto slayers);
	public static partial SlayerBossesDto ToDto(this SlayerBossesDto slayers);

	public static Slayers ToDto(this RawSlayerData? slayers) {
		var result = new Slayers() {
			Xp = 0,
		};

		if (slayers is not null) {
			result.Bosses = new SlayerBosses {
				Blaze = slayers.SlayerBosses?.Blaze?.ToDto(),
				Enderman = slayers.SlayerBosses?.Enderman?.ToDto(),
				Spider = slayers.SlayerBosses?.Spider?.ToDto(),
				Vampire = slayers.SlayerBosses?.Vampire?.ToDto(),
				Wolf = slayers.SlayerBosses?.Wolf?.ToDto(),
				Zombie = slayers.SlayerBosses?.Zombie?.ToDto()
			};
		}
		
		result.Xp = (result.Bosses?.Blaze?.Xp ?? 0) +
		             (result.Bosses?.Enderman?.Xp ?? 0) +
		             (result.Bosses?.Spider?.Xp ?? 0) +
		             (result.Bosses?.Vampire?.Xp ?? 0) +
		             (result.Bosses?.Wolf?.Xp ?? 0) +
		             (result.Bosses?.Zombie?.Xp ?? 0);
		
		return result;
	}
	
	public static SlayerBoss ToDto(this SlayerBossProgress boss) {
		var result = new SlayerBoss() {
			Xp = boss.Xp ?? 0,
			Levels = boss.ClaimedLevels,
			Kills = new Dictionary<string, int>(),
			Attempts = new  Dictionary<string, int>(),
		};
		
		if (boss.BossKillsTier0 is not null) result.Kills["tier_0"] = boss.BossKillsTier0.Value;
		if (boss.BossKillsTier1 is not null) result.Kills["tier_1"] = boss.BossKillsTier1.Value;
		if (boss.BossKillsTier2 is not null) result.Kills["tier_2"] = boss.BossKillsTier2.Value;
		if (boss.BossKillsTier3 is not null) result.Kills["tier_3"] = boss.BossKillsTier3.Value;
		if (boss.BossKillsTier4 is not null) result.Kills["tier_4"] = boss.BossKillsTier4.Value;
		if (boss.BossKillsTier5 is not null) result.Kills["tier_5"] = boss.BossKillsTier5.Value;
	
		if (boss.BossAttemptsTier0 is not null) result.Attempts["tier_0"] = boss.BossAttemptsTier0.Value;
		if (boss.BossAttemptsTier1 is not null) result.Attempts["tier_1"] = boss.BossAttemptsTier1.Value;
		if (boss.BossAttemptsTier2 is not null) result.Attempts["tier_2"] = boss.BossAttemptsTier2.Value;
		if (boss.BossAttemptsTier3 is not null) result.Attempts["tier_3"] = boss.BossAttemptsTier3.Value;
		if (boss.BossAttemptsTier4 is not null) result.Attempts["tier_4"] = boss.BossAttemptsTier4.Value;
		if (boss.BossAttemptsTier5 is not null) result.Attempts["tier_5"] = boss.BossAttemptsTier5.Value;
		
		return result;
	}
}

public class Slayers
{
	public double Xp { get; set; }
	public SlayerBosses? Bosses { get; set; }
}

public class SlayerBosses
{
	public SlayerBoss? Zombie { get; set; }
	public SlayerBoss? Spider { get; set; }
	public SlayerBoss? Wolf { get; set; }
	public SlayerBoss? Enderman { get; set; }
	public SlayerBoss? Blaze { get; set; }
	public SlayerBoss? Vampire { get; set; }
}

public class SlayerBoss
{
	public double Xp { get; set; }
	public Dictionary<string, bool> Levels { get; set; }
	public Dictionary<string, int> Kills { get; set; }
	public Dictionary<string, int> Attempts { get; set; }
}

public class SlayersDto
{
	public double Xp { get; set; }
	public SlayerBossesDto Bosses { get; set; } = new();
}

public class SlayerBossesDto
{
	public SlayerBossDto? Zombie { get; set; }
	public SlayerBossDto? Spider { get; set; }
	public SlayerBossDto? Wolf { get; set; }
	public SlayerBossDto? Enderman { get; set; }
	public SlayerBossDto? Blaze { get; set; }
	public SlayerBossDto? Vampire { get; set; }
}

public class SlayerBossDto
{
	public double Xp { get; set; }
	public Dictionary<string, bool> Levels { get; set; }
	public Dictionary<string, int> Kills { get; set; }
	
	public Dictionary<string, int> Attempts { get; set; }
}