using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Monetization.Services;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Discord;

[DisallowConcurrentExecution]
public class RefreshEntitlementsBackgroundJob(
    IConnectionMultiplexer redis,
    ILogger<RefreshEntitlementsBackgroundJob> logger,
    DataContext context,
    IOptions<ConfigCooldownSettings> coolDowns,
    IMessageService messageService,
    IMonetizationService monetizationService
	) : IJob
{
    public static readonly JobKey Key = new(nameof(RefreshEntitlementsBackgroundJob));
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
	public async Task Execute(IJobExecutionContext executionContext) {
        logger.LogInformation("Confirming entitlements - {UtcNow}", DateTime.UtcNow);

        if (executionContext.RefireCount > 3) {
            messageService.SendErrorMessage("Failed to confirm entitlements", "Retried too many times");
            return;
        }

        try {
            await UpdateExistingEntitlements(executionContext.CancellationToken);
        } catch (Exception e) {
            messageService.SendErrorMessage("Failed to fetch Discord products", e.Message);
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
    }
	
	private async Task UpdateExistingEntitlements(CancellationToken ct) {
        const string key = "bot:check-entitlements";
        var db = redis.GetDatabase();
        if (db.KeyExists(key)) {
            logger.LogInformation("Entitlements are still on cooldown");
            return;
        }
        await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(_coolDowns.EntitlementsRefreshCooldown));

        var accountsToCheck = context.Accounts
            .Include(a => a.MinecraftAccounts)
            .Where(a => a.ActiveRewards)
            .Select(a => a.Id);

        foreach (var accountId in accountsToCheck) {
            if (ct.IsCancellationRequested) break;
            
            await monetizationService.SyncDiscordEntitlementsAsync(accountId, false);
            // await monetizationService.FetchUserEntitlementsAsync(accountId);
            
            await Task.Delay(1000, ct);
        }
        
        await context.SaveChangesAsync(ct);
    }
}