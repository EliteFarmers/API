using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Services.LeaderboardService;
using EliteAPI.Services.MessageService;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace EliteAPI.Background.Profiles;

[DisallowConcurrentExecution]
public class ProcessContestsBackgroundJob(
    IMessageService messageService,
    ILeaderboardService lbService,
    ILogger<ProcessContestsBackgroundJob> logger,
    DataContext context) 
    : IJob
{
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
            await ProcessContests(memberId, incomingJacob);
        }  catch (Exception e) {
            messageService.SendErrorMessage("Failed Process Jacob Contests", e.Message);
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
	}
	
	private async Task ProcessContests(Guid memberId, RawJacobData? incoming) {
        var member = await context.ProfileMembers
            .Include(m => m.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(p => p.JacobContest)
            .FirstOrDefaultAsync(p => p.Id == memberId);

        if (incoming is null) return;

        var jacob = member?.JacobData;
        if (jacob is null || member is null) return;
        
        var incomingContests = incoming?.Contests;
        if (incomingContests is null || incomingContests.Count == 0) return;
        
        // If the last update was 0, we need to do a full refresh
        // This can be triggered manually to correct for errors overtime
        var fullRefresh = jacob.ContestsLastUpdated == 0;
        
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        // Existing contests on the profile
        var existingContests = jacob.Contests
            .DistinctBy(c => c.JacobContest.Timestamp + (int) c.JacobContest.Crop) // Hopefully should not be needed
            .ToDictionary(c => c.JacobContest.Timestamp + (int) c.JacobContest.Crop);
        
        // List of keys to fetch
        var contestsToFetch = incomingContests
            .Where(c => {
                if (c.Value.Collected < 100) return false; // Contests with less than 100 crops collected aren't counted in game
                if (fullRefresh) return true;
                
                var actualKey = FormatUtils.GetTimeFromContestKey(c.Key) + (int)(FormatUtils.GetCropFromContestKey(c.Key) ?? 0);
                // If the contest is new, add it to the list
                if (!existingContests.TryGetValue(actualKey, out var existing)) return true;
                
                // Add if it hasn't been claimed
                return existing.Position <= 0;
            }).ToList();

        var keys = contestsToFetch.Select(c =>
            FormatUtils.GetTimeFromContestKey(c.Key) + (int)(FormatUtils.GetCropFromContestKey(c.Key) ?? 0)
        ).ToList();
        
        // Fetch contests from database
        var fetchedContests = new Dictionary<long, JacobContest>();
            
        try {
            fetchedContests = await context.JacobContests
                .Where(j => keys.Contains(j.Id))
                .ToDictionaryAsync(j => j.Id);
        } catch (Exception e) {
            logger.LogError(e, "Failed to fetch contests from database");
        }
        
        var newParticipations = new List<ContestParticipation>();
        foreach (var (key, contest) in contestsToFetch)
        {
            if (contest.Collected < 100) continue; // Contests with less than 100 crops collected aren't counted in game
            jacob.Participations++;

            var medal = contest.ExtractMedal();
            
            var crop = FormatUtils.GetCropFromContestKey(key);
            if (crop is null) continue;
            
            var actualKey = FormatUtils.GetTimeFromContestKey(key) + (int) crop;

            if (!fetchedContests.TryGetValue(actualKey, out var fetched)) {
                var newContest = new JacobContest {
                    Id = actualKey,
                    Crop = crop.GetValueOrDefault(),
                    Timestamp = FormatUtils.GetTimeFromContestKey(key),
                    Participants = contest.Participants ?? -1,
                    Finnegan = contest.Medal is not null // Only Finnegan contests have a medal set
                };

                try {
                    context.JacobContests.Add(newContest);
                } catch (Exception e) {
                    logger.LogError(e, "Failed to add new contest to database");
                    continue;
                }
                
                fetchedContests.Add(actualKey, newContest);
                fetched = newContest;
            }

            // Update the number of participants if it's not set
            // Should happen infrequently
            if (fetched.Participants == -1 && contest.Participants > 0) {
                if (contest.Medal is not null) {
                    fetched.Finnegan = true;
                }
                
                await context.JacobContests
                    .Where(j => j.Id == actualKey)
                    .ExecuteUpdateAsync(j => 
                        j.SetProperty(c => c.Participants, contest.Participants ?? -1)
                        .SetProperty(c => c.Finnegan, fetched.Finnegan));
            }

            if (!existingContests.TryGetValue(actualKey, out var existing)) {
                // Register new participation
                var newParticipation = new ContestParticipation {
                    Collected = contest.Collected,
                    Position = contest.Position ?? -1,
                    MedalEarned = medal,

                    ProfileMemberId = memberId,
                    ProfileMember = member,
                    JacobContestId = actualKey
                };
                
                fetched.UpdateMedalBracket(newParticipation);
                newParticipations.Add(newParticipation);
            } else if (
                existing.Collected != contest.Collected 
                || (contest.Position is not null && existing.Position != contest.Position) 
                || existing.MedalEarned != medal) 
            {
                await context.ContestParticipations
                    .Where(p => p.ProfileMemberId == memberId && p.JacobContestId == actualKey)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(c => c.Collected, contest.Collected)
                        .SetProperty(c => c.Position, contest.Position ?? -1)
                        .SetProperty(c => c.MedalEarned, medal));
                
                fetched.UpdateMedalBracket(existing);
            }
        }

        context.ContestParticipations.AddRange(newParticipations);
        jacob.Contests.AddRange(newParticipations);

        jacob.Participations = jacob.Contests.Count;

        var medalCounts = await context.JacobData
            .Where(p => p.Id == jacob.Id)
            .SelectMany(p => p.Contests)
            .GroupBy(x => x.MedalEarned)
            .Select(p => new {
                Medal = p.Key,
                Count = p.Count()
            }).ToDictionaryAsync(k => k.Medal, v => v.Count);

        jacob.EarnedMedals.Bronze = medalCounts.TryGetValue(ContestMedal.Bronze, out var bronze) ? bronze : 0;
        jacob.EarnedMedals.Silver = medalCounts.TryGetValue(ContestMedal.Silver, out var silver) ? silver : 0;
        jacob.EarnedMedals.Gold = medalCounts.TryGetValue(ContestMedal.Gold, out var gold) ? gold : 0;
        jacob.EarnedMedals.Platinum = medalCounts.TryGetValue(ContestMedal.Platinum, out var platinum) ? platinum : 0;
        jacob.EarnedMedals.Diamond = medalCounts.TryGetValue(ContestMedal.Diamond, out var diamond) ? diamond : 0;
        
        jacob.FirstPlaceScores = jacob.Contests.Count(c => c.Position == 0);
        
        jacob.ContestsLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        context.JacobData.Update(jacob);
        
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        UpdateLeaderboardPositions(memberId.ToString(), jacob);
	}

    private void UpdateLeaderboardPositions(string memberId, JacobData jacob) {
        lbService.UpdateLeaderboardScore("participations", memberId, jacob.Participations);
        lbService.UpdateLeaderboardScore("firstplace", memberId, jacob.FirstPlaceScores);
            
        lbService.UpdateLeaderboardScore("diamondmedals", memberId, jacob.EarnedMedals.Diamond);
        lbService.UpdateLeaderboardScore("platinummedals", memberId, jacob.EarnedMedals.Platinum);
        lbService.UpdateLeaderboardScore("goldmedals", memberId, jacob.EarnedMedals.Gold);
        lbService.UpdateLeaderboardScore("silvermedals", memberId, jacob.EarnedMedals.Silver);
        lbService.UpdateLeaderboardScore("bronzemedals", memberId, jacob.EarnedMedals.Bronze);
    }
}