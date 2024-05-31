using System.Net.Http.Headers;
using System.Text.Json;
using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Background;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services.DiscordService;

public class DiscordService(
    IHttpClientFactory httpClientFactory,
    DataContext context,
    ILogger<DiscordService> logger,
    IConnectionMultiplexer redis,
    UserManager<ApiUser> userManager,
    IOptions<ConfigCooldownSettings> coolDowns,
    IMapper mapper,
    IBackgroundTaskQueue taskQueue)
    : IDiscordService 
{
    
    private const string ClientName = "EliteAPI";
    private readonly string _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
                                        ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
    private readonly string _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET") 
                                            ?? throw new Exception("DISCORD_CLIENT_SECRET env variable is not set.");
    private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                                        ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");

    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
    private const string DiscordBaseUrl = "https://discord.com/api/v10";

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
            { "scope", "identify guilds role_connections.write" },
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
            Console.WriteLine(e);
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
            { "scope", "identify guilds role_connections.write" },
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
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<EliteAccount?> GetDiscordUser(string accessToken) {
        return await FetchDiscordUser(accessToken);
    }

    public async Task<string> GetGuildMemberPermissions(ulong guildId, ulong userId, string accessToken) {
        var guilds = await GetUsersGuilds(userId, accessToken);
        
        var guild = guilds.FirstOrDefault(g => g.Id.Equals(guildId.ToString()));

        return guild?.Permissions ?? "0";
    }
    
    public async Task<List<UserGuildDto>> GetUsersGuilds(ulong userId, string accessToken) {
        await RefreshBotGuilds();
        
        var url = DiscordBaseUrl + "/users/@me/guilds";
        var key = $"user:guilds:{userId}";
        
        var db = redis.GetDatabase();
        if (db.KeyExists(key)) {
            var guilds = await db.StringGetAsync(key);
            
            if (guilds.IsNullOrEmpty) return new List<UserGuildDto>();

            try {
                var list = JsonSerializer.Deserialize<List<UserGuildDto>>(guilds!);
                if (list is not null) return list;
            } catch (Exception e) {
                logger.LogError(e, "Failed to parse guilds from Redis");
            }
        }
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch guilds from Discord");
            return new List<UserGuildDto>();
        }
        
        try {
            var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>();
            if (list is null) return new List<UserGuildDto>();
            
            var guilds = mapper.Map<List<UserGuildDto>>(list);

            foreach (var guild in guilds) {
                guild.HasBot = await db.KeyExistsAsync($"bot:guild:{guild.Id}");
            }

            await db.StringSetAsync(key, JsonSerializer.Serialize(guilds), TimeSpan.FromSeconds(_coolDowns.UserGuildsCooldown));
            return guilds;
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse guilds from Discord");
            return new List<UserGuildDto>();
        }
    }

    public async Task<UserGuildDto?> GetUserGuildIfManagable(ApiUser user, ulong guildId) {
        if (!user.AccountId.HasValue || user.DiscordAccessToken is null) return null;
        
        var roles = await userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains(ApiUserRoles.Admin);

        var guilds = await GetUsersGuilds(user.AccountId.Value, user.DiscordAccessToken);
        var userGuild = guilds.FirstOrDefault(g => g.Id == guildId.ToString());

        if (isAdmin) {
            return new UserGuildDto {
                Id = guildId.ToString(),
                Permissions = "8",
                Name = string.Empty
            };
        }

        if (userGuild is null || !userGuild.HasGuildAdminPermissions()) {
            return null;
        }

        return userGuild;
    }

    public async Task<FullDiscordGuild?> GetGuild(ulong guildId) {
        await RefreshBotGuilds();
        var db = redis.GetDatabase();
        var key = $"full:guild:{guildId}";

        if (db.KeyExists(key)) {
            var guild = await db.StringGetAsync(key);

            if (!guild.IsNullOrEmpty) {
                try {
                    var data = JsonSerializer.Deserialize<FullDiscordGuild>(guild!);
                    if (data is not null) return data;
                } catch (Exception e) {
                    logger.LogError(e, "Failed to parse guild from Redis");
                }
            }
        }
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(DiscordBaseUrl + $"/guilds/{guildId}?with_counts=true");
        
        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch guild from Discord");
            return null;
        }
        
        try {
            var guild = await response.Content.ReadFromJsonAsync<FullDiscordGuild>();
            if (guild is null) return null;
            
            var channels = await client.GetAsync(DiscordBaseUrl + $"/guilds/{guildId}/channels");
            if (channels.IsSuccessStatusCode) {
                var channelList = await channels.Content.ReadFromJsonAsync<List<DiscordChannel>>();
                
                if (channelList is not null) {
                    guild.Channels = channelList;
                }
            }
            
            await db.StringSetAsync(key, JsonSerializer.Serialize(guild), TimeSpan.FromSeconds(_coolDowns.UserGuildsCooldown));

            var existingGuild = await context.Guilds.FindAsync(ulong.Parse(guild.Id));
            if (existingGuild is null) {
                await RefreshBotGuilds();
            } else {
                existingGuild.Name = guild.Name;
                existingGuild.Icon = guild.Icon;
                existingGuild.DiscordFeatures = guild.Features;
                existingGuild.InviteCode = guild.VanityUrlCode ?? existingGuild.InviteCode;
                existingGuild.Banner = guild.Splash;
                existingGuild.MemberCount = guild.MemberCount;
                
                if (existingGuild.Features.JacobLeaderboardEnabled) {
                    existingGuild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
                }
                
                if (existingGuild.Features.VerifiedRoleEnabled) {
                    existingGuild.Features.VerifiedRole ??= new VerifiedRoleFeature();
                }
            }
            
            await context.SaveChangesAsync();

            return guild;
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse guild from Discord");
            return null;
        }
    }

    public Task RefreshBotGuilds() {
        return Task.CompletedTask;
    }

    private DiscordUpdateResponse? ProcessResponse(RefreshTokenResponse? tokenResponse)
    {
        if (tokenResponse?.AccessToken is null || tokenResponse.RefreshToken is null || tokenResponse.Error is not null) return null;

        DateTimeOffset? accessTokenExpires = tokenResponse.ExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn) : null;
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
    public DateTimeOffset? AccessTokenExpires { get; set; }
    public required string RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpires { get; set; }
    public EliteAccount? Account { get; set; }
}

public static class DiscordExtensions 
{
    public static bool HasGuildAdminPermissions(this UserGuildDto guild) {
        var permissions = guild.Permissions;
        
        if (!ulong.TryParse(permissions, out var bits)) {
            return false;
        }
        
        const ulong admin = 0x8;
        const ulong manageGuild = 0x20;

        // Check if the user has the manage guild or admin permission
        return (bits & admin) == admin || (bits & manageGuild) == manageGuild;
    }
}