using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Profiles;

// Eventually will replace all automapper mappings for profile members
public static class ProfileMemberMapper
{
	public static MemberCosmeticsDto? GetCosmeticsDto(this ProfileMember member) {
		// Only use cosmetics on the primary Minecraft account
		var userSettings = member.MinecraftAccount?.Selected is true
			? member.MinecraftAccount?.EliteAccount?.UserSettings
			: null;
		var cosmetics = member.Metadata?.Cosmetics;

		if (userSettings is null && cosmetics is null) return null;

		var leaderboardCosmetics = cosmetics?.Leaderboard ?? userSettings?.CustomLeaderboardStyle;

		if (userSettings?.LeaderboardStyleId is not null) {
			leaderboardCosmetics ??= new MemberLeaderboardCosmeticsDto();
			leaderboardCosmetics.StyleId = userSettings.LeaderboardStyleId;
		}

		return new MemberCosmeticsDto {
			Prefix = cosmetics?.Prefix ?? userSettings?.Prefix,
			Suffix = cosmetics?.Suffix ?? userSettings?.Suffix,
			Leaderboard = leaderboardCosmetics
		};
	}
}