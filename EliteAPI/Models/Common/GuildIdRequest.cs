using FastEndpoints;
using FluentValidation;
using Newtonsoft.Json;

namespace EliteAPI.Models.Common;

public class GuildIdRequest {
	public required long GuildId { get; set; }
	
	[JsonIgnore]
	public ulong GuildIdUlong => (ulong) GuildId;
}

internal sealed class GuildIdRequestValidator : Validator<GuildIdRequest> {
	public GuildIdRequestValidator() {
		RuleFor(x => x.GuildId)
			.NotEmpty()
			.WithMessage("GuildId is required")
			.GreaterThan(10_000_000_000_000_000)
			.WithMessage("GuildId must be at least 17 digits long and positive");
	}
}