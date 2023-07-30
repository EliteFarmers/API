using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities;

namespace EliteAPI.Services.DiscordService;

public interface IDiscordService
{
    Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken);
    Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken);
    Task<AccountEntity?> GetDiscordUser(string accessToken);
    Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken);
    Task<List<UserGuildDto>> GetUsersGuilds(ulong userId, string accessToken);
    Task<FullDiscordGuild?> GetGuild(ulong guildId);
    Task RefreshBotGuilds();
}
