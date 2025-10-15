using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Account.LinkAccount;

internal sealed class LinkOwnAccountEndpoint(
	IAccountService accountService
) : Endpoint<PlayerRequest, ErrorOr<Success>>
{
	public override void Configure() {
		Post("/account/{Player}");
		Version(0);

		Description(s => s.Accepts<PlayerRequest>());

		Summary(s => { s.Summary = "Link Account"; });
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(PlayerRequest request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);

		return await accountService.LinkAccount(id.Value, request.Player);
	}
}