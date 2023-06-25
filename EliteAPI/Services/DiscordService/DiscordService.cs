using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities;

namespace EliteAPI.Services.DiscordService;

public class DiscordService : IDiscordService
{
    private const string ClientName = "EliteAPI";
    private readonly string _clientId;
    private readonly string _clientSecret;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataContext _context;

    private const string DiscordBaseUrl = "https://discord.com/api";

    public DiscordService(IHttpClientFactory httpClientFactory, DataContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;

        _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
                   ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
        _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET") 
                       ?? throw new Exception("DISCORD_CLIENT_SECRET env variable is not set.");
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

        if (accessToken is null || refreshToken is null) return null;

        var account = await FetchDiscordUser(accessToken);
        if (account is null) return null;

        return new DiscordUpdateResponse()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Account = account
        };
    }

    private async Task<Account?> FetchDiscordUser(string accessToken)
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
                
            var account = existing ?? new Account()
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
            if (refreshTokenResponse?.Error is not null) return null;

            if (refreshTokenResponse?.AccessToken is null || refreshTokenResponse?.RefreshToken is null) return null;

            DateTimeOffset? accessTokenExpires = refreshTokenResponse.ExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(refreshTokenResponse.ExpiresIn) : null;
            var refreshTokenExpires = DateTimeOffset.UtcNow.AddDays(30);

            var data = new DiscordUpdateResponse()
            {
                AccessToken = refreshTokenResponse.AccessToken,
                RefreshToken = refreshTokenResponse.RefreshToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshTokenExpires = refreshTokenExpires,
                Account = null
            };

            return data;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    } 
}

public class DiscordUpdateResponse
{
    public required string AccessToken { get; set; }
    public DateTimeOffset? AccessTokenExpires { get; set; }
    public required string RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpires { get; set; }
    public Account? Account { get; set; }
}