using System.Text.Json.Serialization;
using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Models.Common;

public class ProfileUuidRequest
{
	public required string ProfileUuid { get; set; }

	[JsonIgnore] public string ProfileUuidFormatted => ProfileUuid.ToLowerInvariant().Replace("-", "");
}

internal sealed class ProfileUuidRequestValidator : Validator<ProfileUuidRequest>
{
	public ProfileUuidRequestValidator() {
		RuleFor(x => x.ProfileUuid)
			.NotEmpty()
			.WithMessage("PlayerUuid is required")
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$");
	}
}