using EliteAPI.Models.Entities;

namespace EliteAPI.Services.AccountService;

public interface IAccountService
{
    Task<Account?> GetAccount(int accountId);
    Task<Account?> GetAccountByIGN(string ign);
    Task<Account?> GetAccountByMinecraftUUID(string uuid);
    Task<Account?> GetAccountByDiscordID(ulong id);
    Task<Account?> AddAccount(Account account);
    Task<Account?> UpdateAccount(int id, Account request);
    Task<Account?> DeleteAccount(int id);
}
