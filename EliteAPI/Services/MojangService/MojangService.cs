using EliteAPI.Data;
using EliteAPI.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace EliteAPI.Services.MojangService;

public class MojangService : IMojangService
{
    private const string ClientName = "EliteAPI";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataContext _context;

    public MojangService(IHttpClientFactory httpClientFactory, DataContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByIGN(string ign)
    {
        var account = await _context.MinecraftAccounts
            .Where(mc => mc.Name.Equals(ign))
            .FirstOrDefaultAsync();

        // TODO: Fetch account again if it's older than x amount of time
        if (account == null)
        {
            return await FetchMinecraftAccountByIGN(ign);
        };

        return account;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByUUID(string uuid)
    {
        var account = await _context.MinecraftAccounts
            .Where(mc => mc.Id.Equals(uuid))
            .FirstOrDefaultAsync();

        // TODO: Fetch account again if it's older than x amount of time
        if (account == null)
        {
            return await FetchMinecraftAccountByUUID(uuid);
        };

        return account;
    }

    public async Task<MinecraftAccount?> FetchMinecraftAccountByIGN(string ign)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mojang.com/users/profiles/minecraft/{ign}");
        var client = _httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MojangProfilesResponse>();

            if (data?.Id == null) return null;

            return await FetchMinecraftAccountByUUID(data.Id);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return null;
    }

    public async Task<MinecraftAccount?> FetchMinecraftAccountByUUID(string uuid)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}");
        var client = _httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MinecraftAccount>();

            if (data?.Id == null) return null;

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

    public async Task<MinecraftAccount?> GetMinecraftAccountByUUIDOrIGN(string uuidOrIgn)
    {
        if (uuidOrIgn.Length < 32)
        {
            return await GetMinecraftAccountByIGN(uuidOrIgn);
        }
        else
        {
            return await GetMinecraftAccountByUUID(uuidOrIgn);
        }
    }
}

public class MojangProfilesResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}
