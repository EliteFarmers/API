using EliteAPI.Features.Auth.Models;

namespace EliteAPI.Features.Auth;

public interface IAuthService
{
	Task<AuthResponseDto?> LoginAsync(DiscordLoginDto dto);
	Task<(string token, DateTimeOffset expiry)> GenerateJwtToken(ApiUser user);
	Task<AuthResponseDto?> VerifyRefreshToken(AuthRefreshDto dto);
	Task<AuthResponseDto?> VerifyRefreshToken(string userId, string refreshToken);
	Task TriggerAuthTokenRefresh(string userId);
}