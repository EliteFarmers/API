using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Accounts.UnlinkAccount;

internal sealed class UnlinkAccountEndpoint(
	IAccountService accountService
) : Endpoint<DiscordIdPlayerRequest> {
	
	public override void Configure() {
		Delete("/bot/account/{DiscordId}/{Player}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);
		
		Description(x => x.Accepts<DiscordIdPlayerRequest>());

		Summary(s => {
			s.Summary = "Unlink Account";
		});
	}

	public override async Task HandleAsync(DiscordIdPlayerRequest request, CancellationToken c) {
		var result = await accountService.UnlinkAccount(request.DiscordIdUlong, request.Player);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}

		await SendNoContentAsync(cancellation: c);
	}
}