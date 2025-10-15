using System.Text.Json.Serialization;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Models.Common;

public class PlayerUuidRequest
{
	public required string PlayerUuid { get; set; }

	[JsonIgnore] public string PlayerUuidFormatted => PlayerUuid.ToLowerInvariant().Replace("-", "");
}

internal sealed class PlayerUuidRequestValidator : Validator<PlayerUuidRequest>
{
	public PlayerUuidRequestValidator() {
		RuleFor(x => x.PlayerUuid)
			.NotEmpty()
			.WithMessage("PlayerUuid is required")
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$");
	}
}