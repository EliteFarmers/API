using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Models.Common;

public class PlayerRequest {
	/// <summary>
	/// Player uuid or ign
	/// </summary>
	public required string Player { get; set; }
}

internal sealed class PlayerRequestValidator : Validator<PlayerRequest> {
	public PlayerRequestValidator() {
		RuleFor(x => x.Player)
			.NotEmpty()
			.WithMessage("Player is required")
			.Matches("^[a-zA-Z0-9-_]+$")
			.WithMessage("Player must match ^[a-fA-F0-9-_]+$");
	}
}