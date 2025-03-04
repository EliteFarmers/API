using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Account.SetPrimaryAccount;

internal sealed class SetPrimaryAccountEndpoint(
	IAccountService accountService
) : Endpoint<PlayerRequest> {
	
	public override void Configure() {
		Post("/account/primary/{Player}");
		Version(0);
		
		Description(s => s.Accepts<PlayerRequest>());
		
		Summary(s => {
			s.Summary = "Set Primary Account";
		});
	}

	public override async Task HandleAsync(PlayerRequest request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}
        
		var result = await accountService.MakePrimaryAccount(id.Value, request.Player);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}

		await SendNoContentAsync(cancellation: c);
	}
}