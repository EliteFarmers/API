using FastEndpoints;
using FluentValidation;
using Newtonsoft.Json;

namespace EliteAPI.Models.Common;

public class DiscordIdRequest {
	public required long DiscordId { get; set; }
	
	[JsonIgnore]
	public ulong DiscordIdUlong => (ulong) DiscordId;
}

internal sealed class DiscordIdRequestValidator : Validator<DiscordIdRequest> {
	public DiscordIdRequestValidator() {
		RuleFor(x => x.DiscordId)
			.NotEmpty()
			.WithMessage("DiscordId is required")
			.GreaterThan(10_000_000_000_000_000)
			.WithMessage("DiscordId must be at least 17 digits long and positive");
	}
}