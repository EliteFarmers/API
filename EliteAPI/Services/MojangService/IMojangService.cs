using EliteAPI.Models.Entities;

namespace EliteAPI.Services.MojangService;

public interface IMojangService
{
    public Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid);
    public Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign);
    public Task<MinecraftAccount?> GetMinecraftAccountByUuidOrIgn(string uuidOrIgn);
}
