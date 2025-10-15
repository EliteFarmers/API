using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.SendJacobLeaderboard;

public class SendJacobLeaderboardRequest : DiscordIdRequest
{
	public required string LeaderboardId { get; set; }
}

internal sealed class SendJacobLeaderboardRequestValidator : Validator<SendJacobLeaderboardRequest>
{
	public SendJacobLeaderboardRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}