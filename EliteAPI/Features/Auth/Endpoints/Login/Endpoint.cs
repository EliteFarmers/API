using EliteAPI.Models.DTOs.Auth;
using FastEndpoints;

namespace EliteAPI.Features.Auth.Login;

internal sealed class LoginEndpoint(
	IAuthService authService
) : Endpoint<DiscordLoginDto, AuthResponseDto> {
	public override void Configure() {
		Post("/auth/login");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Log in";
			s.Description = "Log in with discord credentials";
		});
	}

	public override async Task HandleAsync(DiscordLoginDto request, CancellationToken c) {
		var user = await authService.LoginAsync(request);

		if (user is null) {
			await Send.UnauthorizedAsync(c);
			return;
		}

		await Send.OkAsync(user, c);
	}
}