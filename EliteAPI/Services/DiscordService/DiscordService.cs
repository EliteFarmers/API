﻿using System.Net.Http.Headers;
using System.Text.Json;
using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services.DiscordService;

public class DiscordService : IDiscordService
{
    private const string ClientName = "EliteAPI";
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _botToken;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataContext _context;
    private readonly ILogger<DiscordService> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ConfigCooldownSettings _coolDowns;
    private readonly IMapper _mapper;

    private const string DiscordBaseUrl = "https://discord.com/api/v10";

    public DiscordService(IHttpClientFactory httpClientFactory, DataContext context, ILogger<DiscordService> logger, IConnectionMultiplexer redis, IOptions<ConfigCooldownSettings> coolDowns, IMapper mapper)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
        _redis = redis;
        _coolDowns = coolDowns.Value;
        _mapper = mapper;

        _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
                   ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
        _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET") 
                       ?? throw new Exception("DISCORD_CLIENT_SECRET env variable is not set.");
        _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                   ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");
    }

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
        var client = _httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(DiscordBaseUrl + "/users/@me");

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var user = await response.Content.ReadFromJsonAsync<DiscordUserResponse>();

            if (user is null) return null;

            var existing = await _context.Accounts.FindAsync(user.Id);
                
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

            if (existing is null) _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return account;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    private async Task<DiscordUpdateResponse?> RefreshDiscordUser(string refreshToken)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        // application/x-www-form-urlencoded
        var body = new Dictionary<string, string>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "scope", "identify guilds" },
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
        var client = _httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // application/x-www-form-urlencoded
        var body = new Dictionary<string, string>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "authorization_code" },
            { "code", accessToken },
            { "scope", "identify guilds" },
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
        
        var db = _redis.GetDatabase();
        if (db.KeyExists(key)) {
            var guilds = await db.StringGetAsync(key);
            
            if (guilds.IsNullOrEmpty) return new List<UserGuildDto>();

            try {
                var list = JsonSerializer.Deserialize<List<UserGuildDto>>(guilds!);
                if (list is not null) return list;
            } catch (Exception e) {
                _logger.LogError(e, "Failed to parse guilds from Redis");
            }
        }
        
        var client = _httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode) {
            _logger.LogWarning("Failed to fetch guilds from Discord");
            return new List<UserGuildDto>();
        }
        
        try {
            var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>();
            if (list is null) return new List<UserGuildDto>();
            
            var guilds = _mapper.Map<List<UserGuildDto>>(list);

            foreach (var guild in guilds) {
                guild.HasBot = await db.KeyExistsAsync($"bot:guild:{guild.Id}");
            }

            await db.StringSetAsync(key, JsonSerializer.Serialize(guilds), TimeSpan.FromSeconds(_coolDowns.UserGuildsCooldown));
            return guilds;
        } catch (Exception e) {
            _logger.LogError(e, "Failed to parse guilds from Discord");
            return new List<UserGuildDto>();
        }
    }

    public async Task<FullDiscordGuild?> GetGuild(ulong guildId) {
        await RefreshBotGuilds();
        var db = _redis.GetDatabase();
        var key = $"full:guild:{guildId}";

        if (db.KeyExists(key)) {
            var guild = await db.StringGetAsync(key);

            if (!guild.IsNullOrEmpty) {
                try {
                    var data = JsonSerializer.Deserialize<FullDiscordGuild>(guild!);
                    if (data is not null) return data;
                } catch (Exception e) {
                    _logger.LogError(e, "Failed to parse guild from Redis");
                }
            }
        }
        
        var client = _httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(DiscordBaseUrl + $"/guilds/{guildId}?with_counts=true");
        
        if (!response.IsSuccessStatusCode) {
            _logger.LogWarning("Failed to fetch guild from Discord");
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

            var existingGuild = await _context.Guilds.FindAsync(ulong.Parse(guild.Id));
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
            
            await _context.SaveChangesAsync();

            return guild;
        } catch (Exception e) {
            _logger.LogError(e, "Failed to parse guild from Discord");
            return null;
        }
    }

    public async Task RefreshBotGuilds() {
        var db = _redis.GetDatabase();
        if (db.KeyExists("bot:guilds")) return;
        await db.StringSetAsync("bot:guilds", "1", TimeSpan.FromSeconds(_coolDowns.DiscordGuildsCooldown));
        
        var guilds = await FetchBotGuildsRecursive(null);
        
        _logger.LogInformation("Fetched {GuildCount} guilds from Discord", guilds.Count);

        // Allow retry sooner if no guilds were found
        if (guilds.Count == 0) {
            await db.StringGetSetExpiryAsync("bot:guilds", TimeSpan.FromSeconds(60));
        }
        
        var existing = await _context.Guilds.ToListAsync();
        
        foreach (var guild in guilds) {
            var existingGuild = existing.FirstOrDefault(g => g.Id == guild.Id);
            if (existingGuild is null) {
                _context.Guilds.Add(new Guild {
                    Id = guild.Id,
                    Name = guild.Name,
                    Icon = guild.Icon,
                    BotPermissions = guild.Permissions,
                    BotPermissionsNew = guild.PermissionsNew,
                    DiscordFeatures = guild.Features,
                    MemberCount = guild.MemberCount
                });
            } else {
                existingGuild.Name = guild.Name;
                existingGuild.Icon = guild.Icon;
                existingGuild.BotPermissions = guild.Permissions;
                existingGuild.BotPermissionsNew = guild.PermissionsNew;
                existingGuild.DiscordFeatures = guild.Features;
                existingGuild.MemberCount = guild.MemberCount;
            }
            
            await db.StringSetAsync($"bot:guild:{guild.Id}", guild.Permissions, TimeSpan.FromSeconds(_coolDowns.DiscordGuildsCooldown), When.Always);
        }
        
        await _context.SaveChangesAsync();
    }

    private async Task<List<DiscordGuild>> FetchBotGuildsRecursive(string? guildId, List<DiscordGuild>? guilds = null) {
        var url = DiscordBaseUrl + "/users/@me/guilds?with_counts=true";
        
        if (guildId is not null) {
            url += "&after=" + guildId;
        }
        guilds ??= new List<DiscordGuild>();
        
        var client = _httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode) {
            _logger.LogWarning("Failed to fetch bot guilds from Discord");
            return guilds;
        };

        try {
            var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>();

            if (list is null) {
                if (guilds.Count == 0) {
                    _logger.LogWarning("Bot is not in any guilds");
                }
                return guilds;
            }
            
            guilds.AddRange(list);
            
            if (list.Count == 200) {
                return await FetchBotGuildsRecursive(list.Last().Id.ToString(), guilds);
            }
        } catch (Exception e) {
            _logger.LogError(e, "Failed to parse bot guilds from Discord");
        }

        return guilds;
    }

    private DiscordUpdateResponse? ProcessResponse(RefreshTokenResponse? tokenResponse)
    {
        if (tokenResponse?.AccessToken is null || tokenResponse?.RefreshToken is null || tokenResponse.Error is not null) return null;

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