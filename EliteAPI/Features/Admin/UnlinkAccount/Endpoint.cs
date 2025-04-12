using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Admin.UnlinkAccount;

internal sealed class AdminUnlinkAccountRequest {
	public required string DiscordId { get; set; }
	public required string Player { get; set; }
}

internal sealed class UnlinkAccountEndpoint(
	IAccountService accountService
) : Endpoint<AdminUnlinkAccountRequest> {
	
	public override void Configure() {
		Post("/admin/unlink-account");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Unlink an Account";
		});
	}

	public override async Task HandleAsync(AdminUnlinkAccountRequest request, CancellationToken c) {
		if (!ulong.TryParse(request.DiscordId, out var discordId)) {
			ThrowError("Invalid Discord ID", StatusCodes.Status400BadRequest);
			return;
		}
		
		var result = await accountService.UnlinkAccount(discordId, request.Player);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}
		
		if (result is UnauthorizedObjectResult unauthorized) {
			ThrowError(unauthorized.Value?.ToString() ?? "Unauthorized", StatusCodes.Status401Unauthorized);
		}

		await SendNoContentAsync(cancellation: c);
	}
}