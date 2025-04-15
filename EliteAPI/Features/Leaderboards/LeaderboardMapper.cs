using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using Riok.Mapperly.Abstractions;
using Leaderboard = EliteAPI.Configuration.Settings.Leaderboard;
using LeaderboardEntry = EliteAPI.Features.Leaderboards.Services.LeaderboardEntry;

namespace EliteAPI.Features.Leaderboards;

[Mapper]
public static partial class LeaderboardMapper {
	[MapperIgnoreSource(nameof(LeaderboardEntry.MemberId))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.MembersSerializationHelper))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.Removed))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.InitialAmount))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.Meta))]
	[MapperIgnoreTarget(nameof(LeaderboardEntryDto.Mode))]
	public static partial LeaderboardEntryDto MapToDto(this LeaderboardEntry entry);
	
	public static partial LeaderboardEntryWithRankDto MapToDto(this LeaderboardEntryWithRankDto entry);
	
	[MapperIgnoreSource(nameof(Leaderboard.ScoreFormat))]
	[MapperIgnoreSource(nameof(Leaderboard.Order))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.Offset))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.MaxEntries))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.Entries))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.ShortTitle))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.MinimumScore))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.Interval))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.StartsAt))]
	[MapperIgnoreTarget(nameof(LeaderboardDto.EndsAt))]
	public static partial LeaderboardDto MapToDto(this Leaderboard leaderboard);
	
	public static partial ProfileLeaderboardMemberDto MapToDto(this ProfileLeaderboardMember dto);
	
	public static partial MemberCosmeticsDto? MapToDto(this ProfileMemberMetadataCosmetics? meta);
}