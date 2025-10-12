using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Accounts.LinkAccount;

internal sealed class LinkAccountBotEndpoint(
	IAccountService accountService
) : Endpoint<DiscordIdPlayerRequest, ErrorOr<Success>> {
	public override void Configure() {
		Post("/bot/account/{DiscordId}/{Player}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Description(x => x.Accepts<DiscordIdPlayerRequest>());

		Summary(s => { s.Summary = "Link Account"; });
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(DiscordIdPlayerRequest request, CancellationToken c) {
		return await accountService.LinkAccount(request.DiscordIdUlong, request.Player);
	}
}