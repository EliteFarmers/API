using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Account.UnlinkAccount;

internal sealed class UnlinkAccountEndpoint(
	IAccountService accountService
) : Endpoint<PlayerRequest, ErrorOr<Success>> {
	
	public override void Configure() {
		Delete("/account/{Player}");
		Version(0);

		Summary(s => {
			s.Summary = "Unlink Account";
		});
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(PlayerRequest request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}
        
		return await accountService.UnlinkAccount(id.Value, request.Player);
	}
}