using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Auth.Login;

internal sealed class LoginEndpoint(
	IAuthService authService
	) : Endpoint<DiscordLoginDto, AuthResponseDto> 
{
	public override void Configure() {
		Post("/auth/login");
		Version(0);

		Summary(s => {
			s.Summary = "Get logged in account";
			s.Description = "Get the account of the currently logged in user";
		});
	}

	public override async Task HandleAsync(DiscordLoginDto request, CancellationToken c) 
	{
		var user = await authService.LoginAsync(request);
		
		if (user is null) {
			await SendUnauthorizedAsync(cancellation: c);
			return;
		}
		
		await SendAsync(user, cancellation: c);
	}
}