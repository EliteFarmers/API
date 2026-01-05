using EliteAPI.Data;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Profiles.Commands;

public class ProcessMemberDataCommandHandler(
	IServiceScopeFactory scopeFactory,
	IConnectionMultiplexer redis,
	IMessageService messageService,
	ILogger<ProcessMemberDataCommandHandler> logger
) : ICommandHandler<ProcessMemberDataCommand>
{
	public async Task ExecuteAsync(ProcessMemberDataCommand command, CancellationToken ct) {
		var lockKey = $"profile:member:{command.ProfileId}:{command.PlayerUuid}";
		var db = redis.GetDatabase();

		if (!await db.LockTakeAsync(lockKey, "1", TimeSpan.FromMinutes(2))) {
			return;
		}

		try {
			using var scope = scopeFactory.CreateScope();
			
			var context = scope.ServiceProvider.GetRequiredService<DataContext>();
			var profileProcessor = scope.ServiceProvider.GetRequiredService<IProfileProcessorService>();

			var profile = await context.Profiles
				.Include(p => p.Garden)
				.FirstOrDefaultAsync(p => p.ProfileId == command.ProfileId, ct);

			if (profile is null) {
				logger.LogWarning("Profile {ProfileId} not found when processing member {PlayerUuid}", 
					command.ProfileId, command.PlayerUuid);
				return;
			}

			await profileProcessor.ProcessMemberData(
				profile, 
				command.MemberData, 
				command.PlayerUuid, 
				command.RequestedPlayerUuid, 
				command.ProfileData);
			
			await context.SaveChangesAsync(ct);
		}
		catch (Exception e) {
			messageService.SendErrorMessage(
				"Process Member Data Command",
				$"Failed to process member data.\n" +
				$"PlayerId: {command.PlayerUuid}\n" +
				$"ProfileId: {command.ProfileId}\n" +
				$"Error: {e.Message}");
			throw;
		}
		finally {
			await db.LockReleaseAsync(lockKey, "1");
		}
	}
}

