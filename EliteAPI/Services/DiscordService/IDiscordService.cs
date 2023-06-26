using EliteAPI.Models.Entities;

namespace EliteAPI.Services.DiscordService;

public interface IDiscordService
{
    Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken);
    Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken);
}
