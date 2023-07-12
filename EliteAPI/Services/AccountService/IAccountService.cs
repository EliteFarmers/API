using EliteAPI.Models.Entities;

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
    Task<AccountEntity?> AddAccount(AccountEntity account);
    Task<AccountEntity?> DeleteAccount(int id);
}
