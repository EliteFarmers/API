using EliteAPI.Features.Profiles.Services;
using EliteAPI.Services.Interfaces;
using HypixelAPI.DTOs;
using Quartz;

namespace EliteAPI.Background.Profiles;

[DisallowConcurrentExecution]
public class ProcessRemainingMembersBackgroundJob(
    IMessageService messageService,
    IProfileProcessorService profileProcessor)
    : IJob
{
    public static readonly JobKey Key = new(nameof(ProcessRemainingMembersBackgroundJob));
    
	public async Task Execute(IJobExecutionContext executionContext) {
        var playerUuid = executionContext.MergedJobDataMap.GetString("playerUuid");
        if (executionContext.MergedJobDataMap.Get("data") is not ProfilesResponse data || playerUuid is null) return;
        
        if (executionContext.RefireCount > 1) {
            messageService.SendErrorMessage("Process Remaining Members Background Job", 
                "Failed to process remaining members. Refire count exceeded.\n" +
                $"PlayerId: `{playerUuid}`");
            return;
        }

        try {
            await profileProcessor.ProcessRemainingMembers(data, playerUuid);
        }  catch (Exception e) {
            messageService.SendErrorMessage(
                "Process Remaining Members Background Job", 
                "Failed to process remaining members.\n" +
                $"PlayerId: `{playerUuid}`\n" +
                $"Error: `{e.Message}`");
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
	}
}