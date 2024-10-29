using System.Net.Http.Headers;
using System.Security.Claims;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Background.Discord;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Images;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Services;

public class DiscordService(
    IHttpClientFactory httpClientFactory,
    DataContext context,
    ISchedulerFactory schedular,
    ILogger<DiscordService> logger,
    UserManager<ApiUser> userManager,
    IMapper mapper,
    IOptions<ConfigCooldownSettings> coolDowns,
    IObjectStorageService objectStorageService)
    : IDiscordService 
{
    
    private const string ClientName = "EliteAPI";
    private const string Scopes = "identify guilds role_connections.write";
    private const string DiscordBaseUrl = "https://discord.com/api/v10";

    private readonly string _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
                                        ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
    private readonly string _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET") 
                                            ?? throw new Exception("DISCORD_CLIENT_SECRET env variable is not set.");
    private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                                        ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");
    
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
    public async Task<DiscordUpdateResponse?> GetDiscordUser(string? accessToken, string? refreshToken)
    {
        if (accessToken is null && refreshToken is null) return null;

        if (accessToken is null && refreshToken is not null)
        {
            var refreshed = await RefreshDiscordUser(refreshToken);
            if (refreshed is null) return null;

            refreshed.Account = await FetchDiscordUser(refreshed.AccessToken);
            return refreshed;
        }

        if (accessToken is not null && refreshToken is null)
        {
            var refresh = await FetchRefreshToken(accessToken);
            if (refresh is null) return null;

            refresh.Account = await FetchDiscordUser(refresh.AccessToken);
            return refresh;
        }

        if (accessToken is null) return null;
        
        var account = await FetchDiscordUser(accessToken);
        if (account is null) return null;

        return new DiscordUpdateResponse()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken ?? "",
            Account = account
        };
    }

    private async Task<EliteAccount?> FetchDiscordUser(string accessToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(DiscordBaseUrl + "/users/@me");

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var user = await response.Content.ReadFromJsonAsync<DiscordUserResponse>();

            if (user is null) return null;

            var existing = await context.Accounts.FindAsync(user.Id);
                
            var account = existing ?? new EliteAccount()
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName ?? user.Username
            };

            account.Username = user.Username;
            account.DisplayName = user.DisplayName ?? user.Username;
            account.Discriminator = user.Discriminator;
            account.Email = user.Email ?? account.Email;
            account.Avatar = user.Avatar;
            account.Locale = user.Locale;

            if (existing is null) context.Accounts.Add(account);
            await context.SaveChangesAsync();

            return account;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<DiscordUpdateResponse?> RefreshDiscordUser(string refreshToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        // application/x-www-form-urlencoded
        var body = new Dictionary<string, string>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "scope", Scopes },
        };

        var response = await client.PostAsync(DiscordBaseUrl + "/oauth2/token", new FormUrlEncodedContent(body));

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var refreshTokenResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            return ProcessResponse(refreshTokenResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to parse refresh token response from Discord");
            return null;
        }
    }

    public async Task<DiscordUpdateResponse?> FetchRefreshToken(string accessToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // application/x-www-form-urlencoded
        var body = new Dictionary<string, string>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "authorization_code" },
            { "code", accessToken },
            { "scope", Scopes },
        };

        var response = await client.PostAsync(DiscordBaseUrl + "/oauth2/token", new FormUrlEncodedContent(body));

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var tokenResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            return ProcessResponse(tokenResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to parse refresh token response from Discord");
            return null;
        }
    }

    public async Task<EliteAccount?> GetDiscordUser(string accessToken) {
        return await FetchDiscordUser(accessToken);
    }

    public async Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken) {
        var guilds = await GetUsersGuilds(userId.ToString());
        
        var guild = guilds.FirstOrDefault(g => g.Id.Equals(guildId.ToString()));

        return guild?.Permissions ?? "0";
    }

    public async Task<List<GuildMember>> FetchUserGuilds(ApiUser user) {
        const string url = DiscordBaseUrl + "/users/@me/guilds";

        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.DiscordAccessToken);
        
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch User's ({User}) guilds from Discord", user.Id);
            return [];
        }
        
        try {
            var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>();
            if (list is null) return [];

            var memberList = new List<GuildMember>();
            
            foreach (var guild in list) {
                if (!ulong.TryParse(user.Id, out var id)) continue;
                var member = await UpdateGuildMember(id, guild);
                
                if (member is not null) {
                    memberList.Add(member);
                }
            }
            
            // Save changes to guild members
            await context.SaveChangesAsync();
            
            // Delete old guild member entries
            var old = DateTimeOffset.UtcNow.AddSeconds(-_coolDowns.UserGuildsCooldown);
            await context.GuildMembers
                .Where(gm => gm.AccountId == user.Id && gm.LastUpdated < old)
                .DeleteFromQueryAsync();
            
            user.GuildsLastUpdated = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);

            return memberList;
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse guilds from Discord");
            return [];
        }
    }
    
    public async Task<List<GuildMemberDto>> GetUsersGuilds(string userId) {
        var user = await userManager.FindByIdAsync(userId);
        if (user?.DiscordAccessToken is null) return [];
        
        var existing = await context.GuildMembers
            .Include(gm => gm.Guild)
            .ThenInclude(g => g.Icon)
            .Where(gm => gm.AccountId == userId)
            .ToListAsync();
        
        if (existing.Count <= 0 || user.GuildsLastUpdated.OlderThanSeconds(_coolDowns.UserGuildsCooldown)) {
            var jobData = new JobDataMap() {
                { "userId", user.Id }
            };
            
            var sch = await schedular.GetScheduler();
            await sch.TriggerJob(RefreshUserGuildsBackgroundJob.Key, jobData);
        }
        
        return mapper.Map<List<GuildMemberDto>>(existing);
    }
    
    private async Task<GuildMember?> UpdateGuildMember(ulong userId, DiscordGuild guild) {
        var accountId = userId.ToString();
        
        var existingGuild = await context.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guild.Id);
        
        if (existingGuild is null) {
            return null; // We only care about guilds the bot is in
        }
        
        var existing = await context.GuildMembers
            .FirstOrDefaultAsync(gm => gm.GuildId == guild.Id && gm.AccountId == accountId);

        if (existing is null) {
            existing = new GuildMember {
                GuildId = guild.Id,
                Guild = existingGuild,
                AccountId = accountId,
                Permissions = guild.Permissions
            };
            
            context.GuildMembers.Add(existing);
        } else {
            existing.Permissions = guild.Permissions;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }
        
        return existing;
    }
    
    public async Task<GuildMember?> GetGuildMemberIfAdmin(ClaimsPrincipal user, ulong guildId, GuildPermission permission = GuildPermission.Role) {
        var accountId = user.GetId();
        var isAdmin = user.IsInRole(ApiUserPolicies.Admin);

        var member = await GetGuildMember(user, guildId);
        
        // Use fallback admin permissions if the user is an admin but doesn't have the correct permissions
        if (isAdmin && (member is null || !member.HasGuildAdminPermissions(permission))) {
            member ??= new GuildMember {
                AccountId = accountId,
                GuildId = guildId
            };
            
            member.Permissions = 8; // Discord admin permission
            return member;
        }

        if (member is null || !member.HasGuildAdminPermissions(permission)) {
            return null;
        }

        return member;
    }

    public async Task<GuildMember?> GetGuildMember(ClaimsPrincipal user, ulong guildId) {
        var userId = user.GetId();
        if (userId is null) return null;
        
        var member = await context.GuildMembers
            .AsNoTracking()
            .Include(gm => gm.Guild)
            .FirstOrDefaultAsync(gm => gm.AccountId == userId && gm.GuildId == guildId);
        
        if (member is null) {
            return null;
        }

        if (member.LastUpdated.OlderThanSeconds(_coolDowns.UserRolesCooldown)) {
            await GetUsersGuilds(userId);

            await FetchUserRoles(member);
        }
        
        return member;
    }
    
    public async Task FetchUserRoles(GuildMember member) {
        var url = DiscordBaseUrl + $"/guilds/{member.GuildId}/members/{member.AccountId}";

        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch user roles from Discord");
            return;
        }
        
        try {
            var incoming = await response.Content.ReadFromJsonAsync<DiscordGuildMember>();
            if (incoming is null) return;
            
            var roles = incoming.Roles;
            var roleIds = roles.Select(ulong.Parse).ToList();
            
            member.Roles = roleIds;
            member.LastUpdated = DateTimeOffset.UtcNow;
            
            context.GuildMembers.Update(member);
            await context.SaveChangesAsync();
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse user roles from Discord");
        }
    }

    public async Task<Guild?> GetGuild(ulong guildId, bool skipCache = false) {
        var existing = await context.Guilds
            .Include(g => g.Channels)
            .Include(g => g.Roles)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == guildId);
        
        if (!skipCache && existing is not null && !existing.LastUpdated.OlderThanSeconds(_coolDowns.DiscordGuildsCooldown)) {
            return existing;
        }
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(DiscordBaseUrl + $"/guilds/{guildId}?with_counts=true");
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch guild from Discord");
            return existing;
        }
        
        try {
            var guild = await response.Content.ReadFromJsonAsync<FullDiscordGuild>();
            if (guild is null) return existing;
            
            return await UpdateDiscordGuild(guild);
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse guild from Discord");
            return existing;
        }
    }

    public async Task RefreshDiscordGuild(ulong guildId) {
        await GetGuild(guildId, true);
    }

    private async Task<Guild?> UpdateDiscordGuild(FullDiscordGuild? incoming, bool fetchChannels = true) {
        if (incoming is null) return null;
        if (!ulong.TryParse(incoming.Id, out var guildId)) return null;
        
        var guild = await context.Guilds
            .Include(guild => guild.Icon)
            .Include(guild => guild.Banner)
            .FirstOrDefaultAsync(g => g.Id == guildId);

        if (guild is null) {
            var icon = incoming.Icon is not null ? await UpdateGuildIcon(guildId, incoming.Icon) : null;

            context.Guilds.Add(new Guild {
                Id = guildId,
                Name = incoming.Name,
                Icon = icon,
                DiscordFeatures = incoming.Features,
                MemberCount = incoming.MemberCount
            });
        } else {
            guild.Name = incoming.Name;
            guild.DiscordFeatures = incoming.Features;
            guild.InviteCode = incoming.VanityUrlCode ?? guild.InviteCode;
            guild.MemberCount = incoming.MemberCount;
            guild.LastUpdated = DateTimeOffset.UtcNow;

            if (guild.Icon?.Hash != incoming.Icon && incoming.Icon is not null) {
                guild.Icon = await UpdateGuildIcon(guildId, incoming.Icon, guild.Icon?.Hash);
            }
            
            if (guild.IsPublic && guild.Banner?.Hash != incoming.Splash && incoming.Splash is not null) {
                guild.Banner = await UpdateGuildBanner(guildId, incoming.Splash, guild.Banner?.Hash);
            }

            if (guild.Features.JacobLeaderboardEnabled) {
                guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
            }
                
            if (guild.Features.VerifiedRoleEnabled) {
                guild.Features.VerifiedRole ??= new VerifiedRoleFeature();
            }
        }
        
        await context.SaveChangesAsync();

        if (!fetchChannels || guild is null) return guild;
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

        var channels = await client.GetAsync(DiscordBaseUrl + $"/guilds/{incoming.Id}/channels");
        if (!channels.IsSuccessStatusCode) return guild;
        
        var channelList = await channels.Content.ReadFromJsonAsync<List<DiscordChannel>>();
        if (channelList is null) return guild;
        
        foreach (var channel in channelList) {
            await UpdateDiscordChannel(guild, channel, false);
        }
        
        // Remove channels that are no longer in the guild
        var channelIds = channelList.Select(c => ulong.Parse(c.Id)).ToList();
        await context.GuildChannels
            .Where(c => c.GuildId == guild.Id && !channelIds.Contains(c.Id))
            .ExecuteDeleteAsync();
        
        var roles = await client.GetAsync(DiscordBaseUrl + $"/guilds/{incoming.Id}/roles");
        if (!roles.IsSuccessStatusCode) return guild;
        
        var roleList = await roles.Content.ReadFromJsonAsync<List<DiscordRole>>();
        if (roleList is null) return guild;
        
        foreach (var role in roleList) {
            await UpdateDiscordRole(guild, role, false);
        }
        
        // Remove roles that are no longer in the guild
        var roleIds = roleList.Select(r => ulong.Parse(r.Id)).ToList();
        await context.GuildRoles
            .Where(r => r.GuildId == guild.Id && !roleIds.Contains(r.Id))
            .ExecuteDeleteAsync();

        await context.SaveChangesAsync();
        
        return guild;
    }

    public async Task<Image?> UpdateGuildIcon(ulong guildId, string iconHash, string? delete = null) {
        try {
            if (delete is not null) {
                await objectStorageService.DeleteAsync(delete);
            }
            
            var iconType = iconHash.StartsWith("a_") ? "gif" : "webp";

            var icon = await objectStorageService.UploadImageAsync(
                path: $"guilds/{guildId}/icons/{iconHash}.{iconType}",
                remoteUrl: $"https://cdn.discordapp.com/icons/{guildId}/{iconHash}.{iconType}?size=128");

            icon.Hash = iconHash;
            return icon;
        } catch {
            logger.LogWarning("Failed to fetch guild icon from Discord for guild {GuildId}", guildId);
            return null;
        }
    }
    
    public async Task<Image?> UpdateGuildBanner(ulong guildId, string bannerHash, string? delete = null) {
        try {
            if (delete is not null) {
                await objectStorageService.DeleteAsync(delete);
            }
            
            var banner = await objectStorageService.UploadImageAsync(
                path: $"guilds/{guildId}/{bannerHash}.webp",
                remoteUrl: $"https://cdn.discordapp.com/splashes/{guildId}/{bannerHash}.webp?size=1280");

            banner.Hash = bannerHash;
            return banner;
        } catch {
            logger.LogWarning("Failed to fetch guild banner from Discord for guild {GuildId}", guildId);
            return null;
        }
    }
            

    private async Task UpdateDiscordChannel(Guild guild, DiscordChannel channel, bool save = true) {
        if (!ulong.TryParse(channel.Id, out var channelId)) return;
        
        var existingChannel = await context.GuildChannels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.GuildId == guild.Id);
        
        if (existingChannel is null) {
            var newChannel = new GuildChannel {
                GuildId = guild.Id,
                
                Id = channelId,
                Name = channel.Name,
                Position = channel.Position,
                Type = (int) channel.Type
            };
            
            guild.Channels.Add(newChannel);
            context.GuildChannels.Add(newChannel);
        } else {
            existingChannel.Name = channel.Name;
            existingChannel.Position = channel.Position;
            existingChannel.Type = (int) channel.Type;
        }

        if (save) {
            await context.SaveChangesAsync();
        }
    }
    
    private async Task UpdateDiscordRole(Guild guild, DiscordRole role, bool save = true) {
        if (!ulong.TryParse(role.Id, out var roleId)) return;
        
        var existing = guild.Roles.FirstOrDefault(c => c.Id == roleId);
        
        if (existing is null) {
            var newRole = new GuildRole {
                GuildId = guild.Id,
                
                Id = roleId,
                Name = role.Name,
                Position = role.Position,
                Permissions = role.Permissions
            };
            
            guild.Roles.Add(newRole);
            context.GuildRoles.Add(newRole);
        } else {
            existing.Name = role.Name;
            existing.Position = role.Position;
            existing.Permissions = role.Permissions;
        }

        if (save) {
            await context.SaveChangesAsync();
        }
    }

    private DiscordUpdateResponse? ProcessResponse(RefreshTokenResponse? tokenResponse)
    {
        if (tokenResponse?.AccessToken is null || tokenResponse.RefreshToken is null || tokenResponse.Error is not null) return null;

        var accessTokenExpires = tokenResponse.ExpiresIn > 0 
            ? DateTimeOffset.UtcNow.AddMilliseconds(tokenResponse.ExpiresIn) 
            : DateTimeOffset.UtcNow.AddMinutes(8);
        var refreshTokenExpires = DateTimeOffset.UtcNow.AddDays(30);

        return new DiscordUpdateResponse()
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            AccessTokenExpires = accessTokenExpires,
            RefreshTokenExpires = refreshTokenExpires,
            Account = null
        };
    }
}

public class DiscordUpdateResponse
{
    public required string AccessToken { get; set; }
    public DateTimeOffset AccessTokenExpires { get; set; } = DateTimeOffset.UtcNow.AddMinutes(9);
    public required string RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpires { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
    public EliteAccount? Account { get; set; }
}

public static class DiscordExtensions 
{
    private const ulong Admin = 0x8;
    private const ulong ManageGuild = 0x20;
    
    public static bool HasGuildAdminPermissions(this GuildMember member, GuildPermission permission = GuildPermission.Role) {
        var adminRole = member.Guild?.AdminRole ?? 0;
        // Accept admin role as admin if permission is set to role
        if (permission == GuildPermission.Role && adminRole != 0 && member.Roles.Contains(adminRole)) {
            return true;
        }
        
        var bits = member.Permissions;

        if (permission == GuildPermission.Manager) {
            // Accept manage guild as admin if permission is set to manager
            return (bits & Admin) == Admin || (bits & ManageGuild) == ManageGuild;
        }
        
        // Check if the user has the admin permission
        return (bits & Admin) == Admin;
    }
}