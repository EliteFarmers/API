using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Account.UpdateSettings;

internal sealed class UpdateAccountEndpoint(
	IAccountService accountService
) : Endpoint<UpdateUserSettingsDto> {
	
	public override void Configure() {
		Patch("/account/settings");
		Version(0);

		Summary(s => {
			s.Summary = "Update Account Settings";
		});
	}

	public override async Task HandleAsync(UpdateUserSettingsDto request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}
        
		var result = await accountService.UpdateSettings(id.Value, request);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}

		await SendNoContentAsync(cancellation: c);
	}
}