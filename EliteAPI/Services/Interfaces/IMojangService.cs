using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Services.Interfaces;

public interface IMojangService
{
    Task<string?> GetUsernameFromUuid(string uuid);
    Task<string?> GetUuidFromUsername(string username);
    Task<string?> GetUuid(string usernameOrUuid);
    
    Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid);
    Task<MinecraftAccount?> FetchMinecraftAccountByUuid(string uuid);
    Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign);
    Task<MinecraftAccount?> GetMinecraftAccountByUuidOrIgn(string uuidOrIgn);
}
