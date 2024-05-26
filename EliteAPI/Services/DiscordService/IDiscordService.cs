using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Services.DiscordService;

public interface IDiscordService
{
    Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken);
    Task<DiscordUpdateResponse?> RefreshDiscordUser(string refreshToken);
    Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken);
    Task<EliteAccount?> GetDiscordUser(string accessToken);
    Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken);
    Task<List<UserGuildDto>> GetUsersGuilds(ulong userId, string accessToken);
    Task<UserGuildDto?> GetUserGuildIfManagable(ApiUser user, ulong guildId);
    Task<FullDiscordGuild?> GetGuild(ulong guildId);
    Task RefreshBotGuilds();
}
