using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Identity;

namespace EliteAPI.Services.AuthService;

public class AuthService(IDiscordService discordService, UserManager<ApiUser> userManager) : IAuthService {
	public async Task<bool> LoginAsync(DiscordLoginDto dto) {
		var user = await discordService.GetDiscordUser(dto.AccessToken);

		return false;
	}
}