using EliteAPI.Models;

namespace EliteAPI.Services.MojangService;

public interface IMojangService
{
    public Task<MinecraftAccount?> FetchMinecraftAccountByUUID(string uuid);
    public Task<MinecraftAccount?> FetchMinecraftAccountByIGN(string ign);

    public Task<MinecraftAccount?> GetMinecraftAccountByUUID(string uuid);
    public Task<MinecraftAccount?> GetMinecraftAccountByIGN(string ign);
    public Task<MinecraftAccount?> GetMinecraftAccountByUUIDOrIGN(string uuidOrIgn);
}
