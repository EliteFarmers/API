using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.CacheService;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Parsers.Profiles;

public static class JacobContestParser
{
    private static readonly Func<DataContext, long, Task<JacobContest?>> FetchJacobContest = 
        EF.CompileAsyncQuery((DataContext context, long key) =>            
            context.JacobContests
                .FirstOrDefault(j => j.Id == key)
        );

    public static async Task ParseJacobContests(this ProfileMember member, RawJacobData? incomingJacob, DataContext context, ICacheService cache)
    {
        var jacob = member.JacobData;

        var contests = incomingJacob?.Contests;
        if (contests is null) return;

        // Dictionary of existing contests for faster lookup
        var existingContests = jacob.Contests
            .ToDictionary(contest => contest.JacobContest.Timestamp + (int)contest.JacobContest.Crop);

        var newParticipations = new List<ContestParticipation>();
        foreach (var (key, contest) in contests)
        {
            var contestParticipation = await ParseContestParticipation(contest, key, existingContests, jacob, context, cache, member);

            if (contestParticipation is null) continue;
            
            newParticipations.Add(contestParticipation);
        }

        context.ContestParticipations.AddRange(newParticipations);
        member.JacobData.Contests.AddRange(newParticipations);

        jacob.ContestsLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await context.SaveChangesAsync();
    }

    private static async Task<ContestParticipation?> ParseContestParticipation(this RawJacobContest contest,
        string contestKey, Dictionary<long, ContestParticipation> existingContests, JacobData jacob, DataContext context, ICacheService cache, ProfileMember member)
    {
        if (contest.Collected < 100) return null;

        var crop = FormatUtils.GetCropFromContestKey(contestKey);
        if (crop == null) return null;

        jacob.Participations++;

        var medal = contest.ExtractMedal();

        if (medal != ContestMedal.None)
        {
            switch (medal)
            {
                case ContestMedal.Bronze:
                    jacob.EarnedMedals.Bronze++;
                    break;
                case ContestMedal.Silver:
                    jacob.EarnedMedals.Silver++;
                    break;
                case ContestMedal.Gold:
                    jacob.EarnedMedals.Gold++;
                    break;
            }
        }

        var timestamp = FormatUtils.GetTimeFromContestKey(contestKey);
        
        // Only process if the contest is newer than the last updated time
        // or if the contest has not been claimed (in case it got claimed after the last update)
        var existing = existingContests.GetValueOrDefault(timestamp + (int) crop);

        if (existing is not null && timestamp < jacob.ContestsLastUpdated && existing.Position >= 0) return null;

        if (existing is not null)
        {
            existing.Collected = contest.Collected;
            existing.MedalEarned = medal;
            existing.Position = contest.Position ?? -1;

            return null;
        }

        var key = timestamp + (int) crop;

        if (await cache.IsContestUpdateRequired(key)) {
            var jacobContest = await FetchJacobContest(context, key);
            var participants = contest.Participants ?? -1;
            
            if (jacobContest is null)
            {
                jacobContest = new JacobContest
                {
                    Id = key,
                    Timestamp = timestamp,
                    Crop = (Crop) crop,
                    Participants = participants
                };
                context.JacobContests.Add(jacobContest);
            }
            else if (participants > jacobContest.Participants) 
            {
                jacobContest.Participants = participants;
            }
            
            cache.SetContest(key, participants != -1);
        }

        var participation = new ContestParticipation
        {
            Collected = contest.Collected,
            MedalEarned = medal,
            Position = contest.Position ?? -1,

            ProfileMemberId = member.Id,
            ProfileMember = member,
            JacobContestId = key
        };

        return participation;
    }

    public static ContestMedal ExtractMedal(this RawJacobContest contest)
    {
        // Respect given medal if it exists
        if (contest.Medal is not null)
        {
            return contest.Medal switch
            { 
                "gold" => ContestMedal.Gold,
                "silver" => ContestMedal.Silver,
                "bronze" => ContestMedal.Bronze,
                _ => ContestMedal.None
            };
        }

        var participants = contest.Participants;
        var position = contest.Position;
        
        if (position is null || participants is null) return ContestMedal.None;

        // Calculate medal based on position
        if (position <= (participants * 0.05) + 1) return ContestMedal.Gold;
        if (position <= (participants * 0.25) + 1) return ContestMedal.Silver;
        if (position <= (participants * 0.6) + 1) return ContestMedal.Bronze;
        
        return ContestMedal.None;
    }
}
