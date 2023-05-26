using EliteAPI.Data;
using EliteAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.AccountService;

public class AccountService : IAccountService
{

    private readonly DataContext context;
    public AccountService(DataContext context)
    {
        this.context = context;
    }

    public async Task<Account?> AddAccount(Account account)
    {
        await context.Accounts.AddAsync(account);
        await context.SaveChangesAsync();

        return account;
    }

    public async Task<Account?> DeleteAccount(int id)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account == null) return null;

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();

        return account ?? null;
    }

    public async Task<Account?> GetAccount(int accountId)
    {
        return await context.Accounts.FindAsync(accountId);
    }

    public async Task<Account?> GetAccountByDiscordID(ulong id)
    {
        return await context.Accounts
            .Include(account => account.DiscordAccount)
            .Where(account => account.DiscordAccount.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<Account?> GetAccountByIGN(string ign)
    {
        return await context.Accounts
           .Where(account => account.MinecraftAccounts.Exists(mc => mc.IGN.Equals(ign)))
           .FirstOrDefaultAsync();
    }

    public async Task<Account?> GetAccountByMinecraftUUID(string uuid)
    {
        return await context.Accounts
           .Where(account => account.MinecraftAccounts.Exists(mc => mc.UUID.Equals(uuid)))
           .FirstOrDefaultAsync();
    }

    public async Task<Account?> UpdateAccount(int id, Account request)
    {
        if (request == null) return null;

        var account = await context.Accounts.FindAsync(id);
        if (account == null) return null;

        // Update account
        account.MinecraftAccounts = request.MinecraftAccounts;
        account.DiscordAccount = request.DiscordAccount;
        account.PremiumUser = request.PremiumUser;

        await context.SaveChangesAsync();
        return account;
    }
}
