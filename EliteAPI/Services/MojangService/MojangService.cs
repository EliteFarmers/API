using EliteAPI.Data;
using EliteAPI.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using EliteAPI.Config.Settings;
using EliteAPI.Services.CacheService;
using EliteAPI.Utilities;
using Microsoft.Extensions.Options;

namespace EliteAPI.Services.MojangService;

public class MojangService : IMojangService
{
    private const string ClientName = "EliteAPI";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataContext _context;
    private readonly ConfigCooldownSettings _coolDowns;
    private readonly ICacheService _cache;

    public MojangService(IHttpClientFactory httpClientFactory, DataContext context, IOptions<ConfigCooldownSettings> coolDowns, ICacheService cacheService)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _coolDowns = coolDowns.Value;
        _cache = cacheService;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign)
    {
        var account = await _context.MinecraftAccounts
            .Where(mc => mc.Name.Equals(ign))
            .FirstOrDefaultAsync();

        // TODO: Fetch account again if it's older than x amount of time
        if (account is null || account.LastUpdated.OlderThanSeconds(_coolDowns.MinecraftAccountCooldown))
        {
            return await FetchMinecraftAccountByIgn(ign);
        };

        return account;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid)
    {
        var account = await _context.MinecraftAccounts
            .Where(mc => mc.Id.Equals(uuid))
            .FirstOrDefaultAsync();

        if (account is null || account.LastUpdated.OlderThanSeconds(_coolDowns.MinecraftAccountCooldown))
        {
            return await FetchMinecraftAccountByUuid(uuid);
        };

        return account;
    }

    private async Task<MinecraftAccount?> FetchMinecraftAccountByIgn(string ign)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mojang.com/users/profiles/minecraft/{ign}");
        var client = _httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MojangProfilesResponse>();

            if (data?.Id == null) return null;

            return await FetchMinecraftAccountByUuid(data.Id);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return null;
    }

    private async Task<MinecraftAccount?> FetchMinecraftAccountByUuid(string uuid)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}");
        var client = _httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MinecraftAccount>();

            if (data?.Id == null) return null;

            data.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _cache.SetUsernameUuidCombo(data.Name, data.Id);

            await _context.MinecraftAccounts.AddAsync(data);
            await _context.SaveChangesAsync();

            return data;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
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
}

public class MojangProfilesResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}
