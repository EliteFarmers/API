using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.DeleteJacobLeaderboard;

public class DeleteGuildJacobLeaderboardRequest : DiscordIdRequest
{
	public required string LeaderboardId { get; set; }
}

internal sealed class DeleteGuildJacobLeaderboardRequestValidator : Validator<DeleteGuildJacobLeaderboardRequest>
{
	public DeleteGuildJacobLeaderboardRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}