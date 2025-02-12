using System.ComponentModel.DataAnnotations;
using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.UpdateJacobLeaderboard;

public class UpdateJacobLeaderboardRequest : DiscordIdRequest {
	public required string LeaderboardId { get; set; }
	[MaxLength(64)]
	public string? Title { get; set; }

	public string? ChannelId { get; set; }

	public long? StartCutoff { get; set; }
	public long? EndCutoff { get; set; }
	public bool? Active { get; set; } = true;

	public string? RequiredRole { get; set; }
	public string? BlockedRole { get; set; }

	public string? UpdateChannelId { get; set; }
	public string? UpdateRoleId { get; set; }
	public bool? PingForSmallImprovements { get; set; }
}

internal sealed class UpdateJacobLeaderboardRequestValidator : Validator<UpdateJacobLeaderboardRequest> {
	public UpdateJacobLeaderboardRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}