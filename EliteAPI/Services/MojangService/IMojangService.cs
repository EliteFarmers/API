using EliteAPI.Models.Entities;

namespace EliteAPI.Services.MojangService;

public interface IMojangService
{
    public Task<string?> GetUsernameFromUuid(string uuid);
    public Task<string?> GetUuidFromUsername(string username);
    
    public Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid);
    public Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign);
    public Task<MinecraftAccount?> GetMinecraftAccountByUuidOrIgn(string uuidOrIgn);
}
