using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Features.Profiles.Utilities;
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
			var lbService = scope.ServiceProvider.GetRequiredService<ILbService>();

			var profile = await context.Profiles
				.Include(p => p.Garden)
				.FirstOrDefaultAsync(p => p.ProfileId == command.ProfileId, ct);

			if (profile is null) {
				logger.LogWarning("Profile {ProfileId} not found when processing member {PlayerUuid}", 
					command.ProfileId, command.PlayerUuid);
				return;
			}

			var newHash = MemberDataHasher.ComputeHash(command.MemberData);
			var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			// Check if we have an existing member
			var existingMember = await context.ProfileMembers
				.Where(pm => pm.ProfileId == command.ProfileId && pm.PlayerUuid == command.PlayerUuid)
				.Select(pm => new { pm.Id, pm.ResponseHash })
				.FirstOrDefaultAsync(ct);

			if (existingMember is not null) {
				if (existingMember.ResponseHash == newHash && existingMember.ResponseHash != 0) {
					// Ensure interval leaderboard entries exist
					// Disabled for now because of complications with players disabling API access
					// await lbService.EnsureMemberIntervalEntriesExist(existingMember.Id, ct);
					
					await context.ProfileMembers
						.Where(pm => pm.Id == existingMember.Id)
						.ExecuteUpdateAsync(s => s.SetProperty(pm => pm.LastUpdated, now), ct);
					
					logger.LogDebug("Skipped processing for {PlayerUuid} - hash unchanged", command.PlayerUuid);
					return;
				}

				// Data changed
				await profileProcessor.ProcessMemberData(
					profile, 
					command.MemberData, 
					command.PlayerUuid, 
					command.RequestedPlayerUuid, 
					command.ProfileData);
				
				await context.SaveChangesAsync(ct);

				// Update hash and LastDataChanged on the member
				await context.ProfileMembers
					.Where(pm => pm.Id == existingMember.Id)
					.ExecuteUpdateAsync(s => s
						.SetProperty(pm => pm.ResponseHash, newHash)
						.SetProperty(pm => pm.LastDataChanged, pm => pm.LastUpdated), ct);
			} else {
				// New member
				await profileProcessor.ProcessMemberData(
					profile, 
					command.MemberData, 
					command.PlayerUuid, 
					command.RequestedPlayerUuid, 
					command.ProfileData);
				
				await context.SaveChangesAsync(ct);

				await context.ProfileMembers
					.Where(pm => pm.ProfileId == command.ProfileId && pm.PlayerUuid == command.PlayerUuid)
					.ExecuteUpdateAsync(s => s
						.SetProperty(pm => pm.ResponseHash, newHash)
						.SetProperty(pm => pm.LastDataChanged, now), ct);
			}
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
