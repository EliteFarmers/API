using System.Net;
using System.Text.RegularExpressions;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Services;

public partial class MojangService(
    IHttpClientFactory httpClientFactory,
    DataContext context,
    IOptions<ConfigCooldownSettings> coolDowns,
    ICacheService cacheService,
    ILogger<MojangService> logger)
    : IMojangService 
{
    private const string ClientName = "EliteAPI";
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

    public async Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign)
    {
        // Validate that the username is a-z, A-Z, 0-9, _ with a length of 24 or less
        if (!IgnRegex().IsMatch(ign))
        {
            return null;
        }
    
        var account = await context.MinecraftAccounts
            .Include(mc => mc.Badges)
            .Where(mc => mc.Name.Equals(ign))
            .FirstOrDefaultAsync();
        
        if (account is null || account.LastUpdated.OlderThanSeconds(_coolDowns.MinecraftAccountCooldown))
        {
            var fetched = await FetchMinecraftAccountByIgn(ign);
            return fetched ?? account;
        }
        
        // Get the expiry time for the cache with the last updated time in mind
        var expiry = TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - account.LastUpdated));
        cacheService.SetUsernameUuidCombo(account.Name, account.Id, expiry);

        return account;
    }

    public async Task<string?> GetUsernameFromUuid(string uuid) {
        return (await cacheService.GetUsernameFromUuid(uuid)) ?? (await GetMinecraftAccountByUuid(uuid))?.Name;
    }

    public async Task<string?> GetUuidFromUsername(string username) {
        return (await cacheService.GetUuidFromUsername(username)) ?? (await GetMinecraftAccountByIgn(username))?.Id;
    }

    public async Task<string?> GetUuid(string usernameOrUuid) {
        if (usernameOrUuid.Length < 32)
        {
            return await GetUuidFromUsername(usernameOrUuid);
        }

        return usernameOrUuid;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid)
    {
        var account = await context.MinecraftAccounts
            .Where(mc => mc.Id.Equals(uuid))
            .Include(mc => mc.Badges)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (account is null || account.LastUpdated.OlderThanSeconds(_coolDowns.MinecraftAccountCooldown))
        {
            var fetched = await FetchMinecraftAccountByUuid(uuid);
            return fetched ?? account;
        }
        
        // Get the expiry time for the cache with the last updated time in mind
        var expiry = TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - account.LastUpdated));
        cacheService.SetUsernameUuidCombo(account.Name, account.Id, expiry);
        
        context.Entry(account).State = EntityState.Detached;

        return account;
    }

    private readonly string[] _mojangProfileUris = [
        "https://api.minecraftservices.com/minecraft/profile/lookup/name/",
        "https://mowojang.matdoes.dev/users/profiles/minecraft/",
        "https://api.mojang.com/users/profiles/minecraft/"
    ];
    
    private async Task<MinecraftAccount?> FetchMinecraftAccountByIgn(string ign)
    {
        if (!IgnRegex().IsMatch(ign)) return null;
        
        foreach (var uri in _mojangProfileUris)
        {
            var result = await FetchMinecraftUuidByIgn(ign, uri);
            
            // Exit loop if not found, it would be redundant to try other URIs
            if (result.NotFound) {
                return null;
            }

            // Fetch the Minecraft account by UUID if one was returned
            if (result.Id is not null) {
                return await FetchMinecraftAccountByUuid(result.Id);
            }
        }

        return null;
    }
    
    private async Task<(string? Id, bool NotFound)> FetchMinecraftUuidByIgn(string ign, string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri + ign);
        var client = httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound) {
            return (null, true);
        }

        if (!response.IsSuccessStatusCode) {
            return (null, false);
        }

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MojangProfilesResponse>();
            return (data?.Id, false);
        }
        catch (Exception)
        {
            logger.LogWarning("Failed to fetch Minecraft account \"{Ign}\" by IGN at {Uri}", ign, uri);
        }

        return (null, false);
    }

    public async Task<MinecraftAccount?> FetchMinecraftAccountByUuid(string uuid) {
        if (!UuidRegex().IsMatch(uuid)) return null;

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}");
        var client = httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MinecraftAccount>();

            if (data?.Id == null) return null;

            data.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            cacheService.SetUsernameUuidCombo(data.Name, data.Id);
            
            var existing = await context.MinecraftAccounts
                .Include(mc => mc.Badges)
                .Where(mc => mc.Id.Equals(data.Id))
                .FirstOrDefaultAsync();

            if (existing is not null) {
                existing.Name = data.Name;
                existing.Properties = data.Properties;
                existing.LastUpdated = data.LastUpdated;
                
                context.MinecraftAccounts.Update(existing);
                await context.SaveChangesAsync();
                context.Entry(existing).State = EntityState.Detached;
                
                return existing;
            }
            
            await context.MinecraftAccounts.AddAsync(data);
            await context.SaveChangesAsync();

            return data;
        }
        catch (Exception)
        {
            logger.LogWarning("Failed to fetch Minecraft account \"{Uuid}\" by UUID", uuid);
        }

        return null;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByUuidOrIgn(string uuidOrIgn)
    {
        if (uuidOrIgn.Length < 32)
        {
            return await GetMinecraftAccountByIgn(uuidOrIgn);
        }

        return await GetMinecraftAccountByUuid(uuidOrIgn);
    }

    [GeneratedRegex("^[a-zA-Z0-9_]{1,24}$")]
    private static partial Regex IgnRegex();
    
    [GeneratedRegex("^[a-zA-Z0-9_]{32,36}$")]
    private static partial Regex UuidRegex();
}

public class MojangProfilesResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}
