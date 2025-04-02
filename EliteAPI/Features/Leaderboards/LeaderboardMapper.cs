using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Leaderboards;

[Mapper]
public static partial class LeaderboardMapper {
	[MapperIgnoreSource(nameof(LeaderboardEntry.MemberId))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.MembersSerializationHelper))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.Removed))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.InitialAmount))]
	public static partial LeaderboardEntryDto MapToDto(this LeaderboardEntry entry);
	
	public static partial LeaderboardEntryWithRankDto MapToDto(this LeaderboardEntryWithRankDto entry);
	
	[MapperIgnoreSource(nameof(Leaderboard.ScoreFormat))]
	[MapperIgnoreSource(nameof(Leaderboard.Order))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.Offset))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.MaxEntries))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.Entries))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.ShortTitle))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.MinimumScore))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.StartsAt))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.EndsAt))]
	public static partial LeaderboardDto MapToDto(this Leaderboard leaderboard);
	
	public static partial ProfileLeaderboardMemberDto MapToDto(this ProfileLeaderboardMember dto);
}