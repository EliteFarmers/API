using EliteAPI.Models.DTOs.Auth;

namespace EliteAPI.Services.AuthService;

public interface IAuthService {
	Task<bool> LoginAsync(DiscordLoginDto dto);
}