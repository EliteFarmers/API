using System.Security.Claims;
using EliteAPI.Authentication;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;

namespace EliteAPI.Services.Interfaces;

public interface IDiscordService
{
    Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken);
    Task<EliteAccount?> GetDiscordUser(string accessToken);
    Task<DiscordUpdateResponse?> RefreshDiscordUser(string refreshToken);
    Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken);
    Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken);
    Task<List<GuildMemberDto>> GetUsersGuilds(string userId);
    Task<List<GuildMember>> FetchUserGuilds(ApiUser user);
    Task<GuildMember?> GetGuildMember(ClaimsPrincipal user, ulong guildId);
    Task<GuildMember?> GetGuildMemberIfAdmin(ClaimsPrincipal user, ulong guildId, GuildPermission permission = GuildPermission.Role);
    Task FetchUserRoles(GuildMember member);
    Task<Guild?> GetGuild(ulong guildId, bool skipCache = false);
    Task RefreshDiscordGuild(ulong guildId);
}
