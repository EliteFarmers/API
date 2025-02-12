using System.Text.Json.Serialization;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Models.Common;

public class PlayerProfileUuidRequest : PlayerUuidRequest {
	public required string ProfileUuid { get; set; }
	
	[JsonIgnore]
	public string ProfileUuidFormatted => ProfileUuid.ToLowerInvariant().Replace("-", "");
}

internal sealed class PlayerProfileUuidRequestValidator : Validator<PlayerProfileUuidRequest> {
	public PlayerProfileUuidRequestValidator() {
		Include(new PlayerUuidRequestValidator());
		RuleFor(x => x.ProfileUuid)
			.NotEmpty()
			.WithMessage("PlayerUuid is required")
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$");
	}
}