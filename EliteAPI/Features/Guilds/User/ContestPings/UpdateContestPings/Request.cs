using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Guilds.User.ContestPings.UpdateContestPings;

public class UpdateContestPingsRequest : DiscordIdRequest 
{
	[FromBody]
	public required UpdateContestPings Settings { get; set; }
	
	public class UpdateContestPings {
		/// <summary>
		/// Indicates whether the contest pings feature is enabled for the guild.
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Channel ID to send contest pings to.
		/// </summary>
		public string? ChannelId { get; set; }

		/// <summary>
		/// Role ID to ping when a contest starts.
		/// </summary>
		public string? AlwaysPingRole { get; set; }

		/// <summary>
		/// Individual roles to ping when a contest for a specific crop starts.
		/// </summary>
		public CropSettings<string>? CropPingRoles { get; set; } = new();

		/// <summary>
		/// Not in use yet. Delay in seconds before sending the ping.
		/// </summary>
		public int DelaySeconds { get; set; }

		/// <summary>
		/// Reason for disabling the feature.
		/// </summary>
		public string? DisabledReason { get; set; }
	}
}

internal sealed class UpdateContestPingsRequestValidator : Validator<UpdateContestPingsRequest> {
	public UpdateContestPingsRequestValidator() {
		Include(new DiscordIdRequestValidator());
		
		RuleFor(x => x.Settings.DisabledReason)
			.MaximumLength(128)
			.WithMessage("Reason must be 128 characters or less.")
			.When(x => !string.IsNullOrWhiteSpace(x.Settings.DisabledReason));
	}
}