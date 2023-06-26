using EliteAPI.Models.Entities;

namespace EliteAPI.Services.AccountService;

public interface IAccountService
{
    Task<AccountEntities?> GetAccount(ulong accountId);
    Task<AccountEntities?> GetAccountByIgn(string ign);
    Task<AccountEntities?> GetAccountByMinecraftUuid(string uuid);
    Task<AccountEntities?> AddAccount(AccountEntities account);
    Task<AccountEntities?> UpdateAccount(int id, AccountEntities request);
    Task<AccountEntities?> DeleteAccount(int id);
}
