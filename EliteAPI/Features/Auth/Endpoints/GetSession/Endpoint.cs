using System.Security.Claims;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Auth;
using FastEndpoints;

namespace EliteAPI.Features.Auth.GetSession;

internal sealed class GetSessionEndpoint() : EndpointWithoutRequest<AuthSessionDto> {
	public override void Configure() {
		Get("/auth/me");
		Version(0);

		Summary(s => {
			s.Summary = "Get logged in account";
			s.Description = "Get the account of the currently logged in user";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		await Send.OkAsync(new AuthSessionDto {
			Id = User.FindFirstValue(ClaimNames.NameId) ?? string.Empty,
			Username = User.FindFirstValue(ClaimNames.Name) ?? string.Empty,
			Avatar = User.FindFirstValue(ClaimNames.Avatar) ?? string.Empty,
			Ign = User.FindFirstValue(ClaimNames.Ign) ?? string.Empty,
			FIgn = User.FindFirstValue(ClaimNames.FormattedIgn) ?? string.Empty,
			Uuid = User.FindFirstValue(ClaimNames.Uuid) ?? string.Empty,
			Roles = User.Claims.Where(l => l.Type == ClaimNames.Role).Select(a => a.Value).ToArray()
		}, c);
	}
}