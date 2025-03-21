using System.Security.Claims;
using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Auth.GetSelf;

internal sealed class GetSelfEndpoint(
	IAuthService authService
	) : EndpointWithoutRequest<AuthSessionDto> 
{
	public override void Configure() {
		Get("/auth/me");
		Version(0);

		Summary(s => {
			s.Summary = "Get logged in account";
			s.Description = "Get the account of the currently logged in user";
		});
	}

	public override async Task HandleAsync(CancellationToken c) 
	{
		var id = User.GetId();
		if (id is not null && User.AccessTokenExpired()) {
			await authService.TriggerAuthTokenRefresh(id);
		}
		
		await SendAsync(new AuthSessionDto {
			Id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
			Username = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
			Avatar = User.FindFirstValue(ApiUserClaims.Avatar) ?? string.Empty,
			Ign = User.FindFirstValue(ApiUserClaims.Ign) ?? string.Empty,
			Uuid = User.FindFirstValue(ApiUserClaims.Uuid) ?? string.Empty,
			Roles = User.Claims.Where(l => l.Type == ClaimTypes.Role).Select(a => a.Value).ToArray()
		}, cancellation: c);
	}
}