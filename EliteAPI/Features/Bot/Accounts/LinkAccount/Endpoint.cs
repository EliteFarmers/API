using EliteAPI.Authentication;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Accounts.LinkAccount;

internal sealed class LinkAccountEndpoint(
	IAccountService accountService
) : Endpoint<DiscordIdPlayerRequest> {
	
	public override void Configure() {
		Post("/bot/account/{DiscordId}/{Player}");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Link Account";
		});
	}

	public override async Task HandleAsync(DiscordIdPlayerRequest request, CancellationToken c) {
		var result = await accountService.LinkAccount(request.DiscordIdUlong, request.Player);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}

		await SendOkAsync(cancellation: c);
	}
}