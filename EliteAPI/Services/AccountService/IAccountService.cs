using EliteAPI.Models.Entities;

namespace EliteAPI.Services.AccountService;

public interface IAccountService
{
    /// <summary>
    /// Gets an account by the linked Discord account Id
    /// </summary>
    /// <param name="accountId">Linked Discord account Id</param>
    /// <returns></returns>
    Task<AccountEntities?> GetAccount(ulong accountId);
    Task<AccountEntities?> GetAccountByIgn(string ign);
    Task<AccountEntities?> GetAccountByMinecraftUuid(string uuid);
    Task<AccountEntities?> AddAccount(AccountEntities account);
    Task<AccountEntities?> UpdateAccount(int id, AccountEntities request);
    Task<AccountEntities?> DeleteAccount(int id);
}
