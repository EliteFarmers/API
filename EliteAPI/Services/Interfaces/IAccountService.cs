using EliteAPI.Models.Entities.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Gets an account by the linked Discord account Id
    /// </summary>
    /// <param name="accountId">Linked Discord account Id</param>
    /// <returns></returns>
    Task<EliteAccount?> GetAccount(ulong accountId);
    Task<EliteAccount?> GetAccountByIgn(string ign);
    Task<EliteAccount?> GetAccountByMinecraftUuid(string uuid);
    Task<EliteAccount?> GetAccountByIgnOrUuid(string ignOrUuid);
    Task<ActionResult> LinkAccount(ulong discordId, string playerUuidOrIgn);
    Task<ActionResult> UnlinkAccount(ulong discordId, string playerUuidOrIgn);
    Task<ActionResult> MakePrimaryAccount(ulong discordId, string playerUuidOrIgn);
}
