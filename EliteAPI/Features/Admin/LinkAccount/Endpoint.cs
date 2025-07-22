using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Admin.LinkAccount;

internal sealed class AdminLinkAccountRequest {
	public required string DiscordId { get; set; }
	public required string Player { get; set; }
}

internal sealed class LinkAccountEndpoint(
	IAccountService accountService
) : Endpoint<AdminLinkAccountRequest> {
	
	public override void Configure() {
		Post("/admin/link-account");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Link an Account";
		});
	}

	public override async Task HandleAsync(AdminLinkAccountRequest request, CancellationToken c) {
		if (!ulong.TryParse(request.DiscordId, out var discordId)) {
			ThrowError("Invalid Discord ID", StatusCodes.Status400BadRequest);
			return;
		}

		var result = await accountService.LinkAccount(discordId, request.Player);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}
		
		if (result is UnauthorizedObjectResult unauthorized) {
			ThrowError(unauthorized.Value?.ToString() ?? "Unauthorized", StatusCodes.Status401Unauthorized);
		}

		await SendNoContentAsync(cancellation: c);
	}
}