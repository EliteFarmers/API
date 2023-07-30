using EliteAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.AccountService;

public interface IAccountService
{
    /// <summary>
    /// Gets an account by the linked Discord account Id
    /// </summary>
    /// <param name="accountId">Linked Discord account Id</param>
    /// <returns></returns>
    Task<AccountEntity?> GetAccount(ulong accountId);
    Task<AccountEntity?> GetAccountByIgn(string ign);
    Task<AccountEntity?> GetAccountByMinecraftUuid(string uuid);
    Task<AccountEntity?> GetAccountByIgnOrUuid(string ignOrUuid);
    Task<ActionResult> LinkAccount(ulong discordId, string playerUuidOrIgn);
    Task<ActionResult> UnlinkAccount(ulong discordId, string playerUuidOrIgn);
    Task<ActionResult> MakePrimaryAccount(ulong discordId, string playerUuidOrIgn);
}
