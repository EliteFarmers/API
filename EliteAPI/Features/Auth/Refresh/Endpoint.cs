using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Auth.Refresh;

internal sealed class RefreshEndpoint(
	IAuthService authService
	) : Endpoint<AuthRefreshDto, AuthResponseDto> 
{
	public override void Configure() {
		Post("/auth/refresh");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Refresh Auth Token";
			s.Description = "Refresh the auth token using the refresh token";
		});
	}

	public override async Task HandleAsync(AuthRefreshDto request, CancellationToken c) 
	{
		var response = await authService.VerifyRefreshToken(request);
		
		if (response is null) {
			await SendUnauthorizedAsync(cancellation: c);
			return;
		}
		
		await SendAsync(response, cancellation: c);
	}
}