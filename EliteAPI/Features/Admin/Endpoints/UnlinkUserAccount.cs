using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Features.Admin.Endpoints;

internal sealed class AdminUnlinkAccountRequest
{
	public required string DiscordId { get; set; }
	public required string Player { get; set; }
}

internal sealed class UnlinkUserAccountEndpoint(
	IAccountService accountService
) : Endpoint<AdminUnlinkAccountRequest, ErrorOr<Success>>
{
	public override void Configure() {
		Post("/admin/unlink-account");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => { s.Summary = "Unlink an Account"; });
	}

	public override async Task<ErrorOr<Success>> ExecuteAsync(AdminUnlinkAccountRequest request, CancellationToken c) {
		if (!ulong.TryParse(request.DiscordId, out var discordId))
			ThrowError("Invalid Discord ID", StatusCodes.Status400BadRequest);

		return await accountService.UnlinkAccount(discordId, request.Player);
	}
}