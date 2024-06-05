using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;

namespace EliteAPI.Services.DiscordService;

public interface IDiscordService
{
    Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken);
    Task<EliteAccount?> GetDiscordUser(string accessToken);
    Task<DiscordUpdateResponse?> RefreshDiscordUser(string refreshToken);
    Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken);
    Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken);
    Task<List<UserGuildDto>> GetUsersGuilds(ulong userId, string accessToken);
    Task<GuildMember?> GetGuildMember(ApiUser user, ulong guildId);
    Task<GuildMember?> GetGuildMemberIfAdmin(ApiUser user, ulong guildId);
    Task FetchUserRoles(GuildMember member);
    Task<Guild?> GetGuild(ulong guildId, bool skipCache = false);
    Task RefreshDiscordGuild(ulong guildId);
}
