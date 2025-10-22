using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.UpdateJacob;

public class UpdateJacobFeatureRequest : DiscordIdRequest
{
	[QueryParam] public string? Reason { get; set; }

	[FromBody] public required UpdateJacobFeature Feature { get; set; }

	public class UpdateJacobFeature
	{
		/// <summary>
		/// Blocked roles from participating in the guild's Jacob Leaderboards
		/// </summary>
		public List<DiscordRole> BlockedRoles { get; set; } = [];

		/// <summary>
		/// Blocked users from participating in the guild's Jacob Leaderboards
		/// </summary>
		public List<ulong> BlockedUsers { get; set; } = [];

		/// <summary>
		/// Required roles to participate in the guild's Jacob Leaderboards
		/// </summary>
		public List<DiscordRole> RequiredRoles { get; set; } = [];

		/// <summary>
		/// Excluded participations from the guild's Jacob Leaderboards
		/// </summary>
		public List<string> ExcludedParticipations { get; set; } = [];

		/// <summary>
		/// Excluded timespans from the guild's Jacob Leaderboards
		/// </summary>
		public List<ExcludedTimespan> ExcludedTimespans { get; set; } = [];

		/// <summary>
		/// Leaderboards for the guild's Jacob Leaderboards
		/// </summary>
		public List<GuildJacobLeaderboard> Leaderboards { get; set; } = [];
	}
}

internal sealed class UpdateJacobFeatureRequestValidator : Validator<UpdateJacobFeatureRequest>
{
	public UpdateJacobFeatureRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}