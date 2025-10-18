using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Account.UpdateFortuneSettings;

internal sealed class UpdateFortuneSettingsEndpoint(
	IAccountService accountService
) : Endpoint<UpdateFortuneSettings, ErrorOr<Success>>
{
	public override void Configure() {
		Post("/account/{PlayerUuid}/{ProfileUuid}/fortune");
		Version(0);

		Summary(s => { s.Summary = "Update Fortune Settings for Account"; });
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(UpdateFortuneSettings request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) return Error.Unauthorized();

		return await accountService.UpdateFortuneSettings(id.Value, request.PlayerUuidFormatted,
			request.ProfileUuidFormatted, request.Settings);
	}
}

internal sealed class UpdateFortuneSettings : PlayerProfileUuidRequest
{
	[FromBody] public required MemberFortuneSettingsDto Settings { get; set; }
}

internal sealed class UpdateFortuneSettingsValidator : Validator<UpdateFortuneSettings>
{
	public UpdateFortuneSettingsValidator() {
		Include(new PlayerProfileUuidRequestValidator());
	}
}