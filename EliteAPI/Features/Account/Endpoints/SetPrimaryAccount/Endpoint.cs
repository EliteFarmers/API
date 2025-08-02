using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Account.SetPrimaryAccount;

internal sealed class SetPrimaryAccountEndpoint(
	IAccountService accountService
) : Endpoint<PlayerRequest, ErrorOr<Success>> {
	
	public override void Configure() {
		Post("/account/primary/{Player}");
		Version(0);
		
		Description(s => s.Accepts<PlayerRequest>());
		
		Summary(s => {
			s.Summary = "Set Primary Account";
		});
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(PlayerRequest request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}
        
		return await accountService.MakePrimaryAccount(id.Value, request.Player);
	}
}