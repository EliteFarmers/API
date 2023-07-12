using EliteAPI.Data;
using EliteAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.AccountService;

public class AccountService : IAccountService
{

    private readonly DataContext _context;
    public AccountService(DataContext context)
    {
        _context = context;
    }

    public Task<AccountEntity?> GetAccountByIgnOrUuid(string ignOrUuid) {
        return ignOrUuid.Length == 32 ? GetAccountByMinecraftUuid(ignOrUuid) : GetAccountByIgn(ignOrUuid);
    }

    public async Task<AccountEntity?> AddAccount(AccountEntity account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }

    public async Task<AccountEntity?> DeleteAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null) return null;

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return account ?? null;
    }

    public async Task<AccountEntity?> GetAccount(ulong accountId) {
        return await _context.Accounts
            .Include(a => a.MinecraftAccounts)
            .FirstOrDefaultAsync(a => a.Id == accountId);
    }

    public async Task<AccountEntity?> GetAccountByIgn(string ign)
    {
        var minecraftAccount = await _context.MinecraftAccounts
            .FirstOrDefaultAsync(mc => mc.Name.Equals(ign));
        
        if (minecraftAccount?.AccountId is null) return null;

        return await GetAccount(minecraftAccount.AccountId ?? 0);
    }

    public async Task<AccountEntity?> GetAccountByMinecraftUuid(string uuid)
    {
        var minecraftAccount = await _context.MinecraftAccounts
            .FirstOrDefaultAsync(mc => mc.Id.Equals(uuid));
        
        if (minecraftAccount?.AccountId is null) return null;

        return await GetAccount(minecraftAccount.AccountId ?? 0);
    }
}
