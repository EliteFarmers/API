using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Accounts.UnlinkAccount;

internal sealed class UnlinkAccountEndpoint(
	IAccountService accountService
) : Endpoint<DiscordIdPlayerRequest, ErrorOr<Success>> {
	
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

	public override async Task<ErrorOr<Success>> ExecuteAsync(DiscordIdPlayerRequest request, CancellationToken c) {
		return await accountService.UnlinkAccount(request.DiscordIdUlong, request.Player);
	}
}