using EliteAPI.Models.Entities;

namespace EliteAPI.Services.AccountService;

public interface IAccountService
{
    Task<Account?> GetAccount(ulong accountId);
    Task<Account?> GetAccountByIgn(string ign);
    Task<Account?> GetAccountByMinecraftUuid(string uuid);
    Task<Account?> AddAccount(Account account);
    Task<Account?> UpdateAccount(int id, Account request);
    Task<Account?> DeleteAccount(int id);
}
