using EliteAPI.Models.Entities;

namespace EliteAPI.Services.MojangService;

public interface IMojangService
{
    public Task<MinecraftAccount?> FetchMinecraftAccountByUuid(string uuid);
    public Task<MinecraftAccount?> FetchMinecraftAccountByIgn(string ign);

    public Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid);
    public Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign);
    public Task<MinecraftAccount?> GetMinecraftAccountByUuidOrIgn(string uuidOrIgn);

    public Task<string?> GetUuidFromIgn(string ign);
    public Task<string?> GetIgnFromUuid(string uuid);
}
