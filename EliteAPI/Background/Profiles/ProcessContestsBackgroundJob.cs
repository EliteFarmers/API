using EliteAPI.Features.Profiles.Services;
using EliteAPI.Services.Interfaces;
using EliteFarmers.HypixelAPI.DTOs;
using Quartz;

namespace EliteAPI.Background.Profiles;

[DisallowConcurrentExecution]
public class ProcessContestsBackgroundJob(
	IMessageService messageService,
	IContestsProcessorService contestsProcessorService)
	: IJob {
	public static readonly JobKey Key = new(nameof(ProcessContestsBackgroundJob));

	public async Task Execute(IJobExecutionContext executionContext) {
		var accountId = executionContext.MergedJobDataMap.GetString("AccountId");
		var profileId = executionContext.MergedJobDataMap.GetString("ProfileId");
		var memberId = executionContext.MergedJobDataMap.GetGuidValue("MemberId");
		var incomingJacob = executionContext.MergedJobDataMap.Get("Jacob") as RawJacobData;

		// logger.LogInformation("Processing {Count} Jacob contests for {AccountId} - {ProfileId}", incomingJacob?.Contests.Count ?? 0, accountId, profileId);

		if (executionContext.RefireCount > 1) {
			messageService.SendErrorMessage("Process Contests Background Job",
				"Failed to process Jacob contests. Refire count exceeded.\n" +
				$"AccountId: `{accountId}`\nProfileId: {profileId}\n[Profile](<https://elitebot.dev/@{accountId}/{profileId}>)");
			return;
		}

		try {
			await contestsProcessorService.ProcessContests(memberId, incomingJacob);
		}
		catch (Exception e) {
			messageService.SendErrorMessage(
				"Failed Process Jacob Contests",
				$"AccountId: `{accountId}`\nProfileId: {profileId}\n[Profile](<https://elitebot.dev/@{accountId}/{profileId}>)\n" +
				e.Message);
			throw new JobExecutionException("", refireImmediately: true, cause: e);
		}
	}
}