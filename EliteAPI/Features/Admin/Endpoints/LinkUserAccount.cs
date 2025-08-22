using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Admin.Endpoints;

internal sealed class AdminLinkAccountRequest {
	public required string DiscordId { get; set; }
	public required string Player { get; set; }
}

internal sealed class LinkUserAccountEndpoint(
	IAccountService accountService
) : Endpoint<AdminLinkAccountRequest, ErrorOr<Success>> {
	
	public override void Configure() {
		Post("/admin/link-account");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Link an Account";
		});
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(AdminLinkAccountRequest request, CancellationToken c) {
		if (!ulong.TryParse(request.DiscordId, out var discordId)) {
			ThrowError("Invalid Discord ID", StatusCodes.Status400BadRequest);
		}

		return await accountService.LinkAccount(discordId, request.Player);
	}
}