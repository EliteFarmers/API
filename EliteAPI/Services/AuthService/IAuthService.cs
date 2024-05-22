﻿using EliteAPI.Models.DTOs.Auth;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Services.AuthService;

public interface IAuthService {
	Task<AuthResponseDto?> LoginAsync(DiscordLoginDto dto);
	Task<(string token, DateTimeOffset expiry)> GenerateJwtToken(ApiUser user);
	Task<AuthResponseDto?> VerifyRefreshToken(AuthResponseDto dto);
}