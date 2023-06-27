namespace EliteAPI.Services.CacheService;

public interface ICacheService
{
    public Task<string?> GetUsernameFromUuid(string uuid);
    public Task<string?> GetUuidFromUsername(string username);

    public void SetUsernameUuidCombo(string username, string uuid);
}
