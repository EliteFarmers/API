using EliteAPI.Data;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace EliteAPI.Background.Discord;

[DisallowConcurrentExecution]
public class CleanupRefreshTokens(
	IMessageService messageService,
	DataContext context)
	: IJob {
	public static readonly JobKey Key = new(nameof(CleanupRefreshTokens));

	public async Task Execute(IJobExecutionContext executionContext) {
		if (executionContext.RefireCount > 2) {
			messageService.SendErrorMessage("Refresh Token Cleanup Failed",
				"Failed to cleanup refresh tokens. Refire count exceeded.");
			return;
		}

		try {
			await context.RefreshTokens
				.Where(r => r.ExpiresUtc < DateTime.UtcNow)
				.ExecuteDeleteAsync();
		}
		catch (Exception e) {
			messageService.SendErrorMessage("Refresh Token Cleanup Failed", e.Message);
			throw new JobExecutionException("", refireImmediately: true, cause: e);
		}
	}
}