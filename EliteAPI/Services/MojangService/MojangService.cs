using EliteAPI.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace EliteAPI.Services.MojangService;

public class MojangService : IMojangService
{
    private readonly string ClientName = "EliteAPI";
    private readonly IHttpClientFactory httpClientFactory;
    private readonly DataContext context;

    public MojangService(IHttpClientFactory httpClientFactory, DataContext context)
    {
        this.httpClientFactory = httpClientFactory;
        this.context = context;
    }

    public async Task<MinecraftAccount?> GetMinecraftAccountByIGN(string ign)
    {
        var account = await context.MinecraftAccounts
            .Where(mc => mc.IGN.Equals(ign))
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
        var account = await context.MinecraftAccounts
            .Where(mc => mc.UUID.Equals(uuid))
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
        var client = httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MojangProfilesResponse>();

            if (data == null || data.Id == null) return null;

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
        var client = httpClientFactory.CreateClient(ClientName);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var data = await response.Content.ReadFromJsonAsync<MinecraftAccount>();

            if (data == null || data.Id == null) return null;

            await context.MinecraftAccounts.AddAsync(data);
            await context.SaveChangesAsync();

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
