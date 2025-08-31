using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guilds.User.Jacob.CreateJacobLeaderboard;

public class CreateJacobLeaderboardRequest : DiscordIdRequest 
{
	[FromBody]
	public required CreateJacobLeaderboard Leaderboard { get; set; }
	
	public class CreateJacobLeaderboard {
		public required string Title { get; set; }

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
}

internal sealed class CreateJacobFeatureRequestValidator : Validator<CreateJacobLeaderboardRequest> {
	public CreateJacobFeatureRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.Leaderboard.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(64)
			.WithMessage("Title must not exceed 64 characters.");
	}
}