using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.CacheService;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Parsers.Profiles;

public static class JacobContestParser
{
    public static async Task ParseJacobContests(this ProfileMember member, RawJacobData? incomingJacob, DataContext context, ICacheService cache)
    {
        var jacob = member.JacobData;

        var incomingContests = incomingJacob?.Contests;
        if (incomingContests is null || incomingContests.Count == 0) return;
        
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        // Existing contests on the profile
        var existingContests = jacob.Contests
            .DistinctBy(c => c.JacobContest.Timestamp + (int) c.JacobContest.Crop) // Hopefully should not be needed
            .ToDictionary(c => c.JacobContest.Timestamp + (int) c.JacobContest.Crop);
        
        // List of keys to fetch
        var contestsToFetch = incomingContests
            .Where(c => {
                var actualKey = FormatUtils.GetTimeFromContestKey(c.Key) 
                                + (int)(FormatUtils.GetCropFromContestKey(c.Key) ?? 0);
                
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
            Console.Error.WriteLine(e);
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
                    Participants = contest.Participants ?? -1
                };

                try {
                    context.JacobContests.Add(newContest);
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
                
                fetchedContests.Add(actualKey, newContest);
                fetched = newContest;
            }

            // Update the number of participants if it's not set
            // Should happen infrequently
            if (fetched.Participants == -1 && contest.Participants > 0) {
                await context.JacobContests
                    .Where(j => j.Id == actualKey)
                    .ExecuteUpdateAsync(j => j.SetProperty(c => c.Participants, contest.Participants ?? -1));
            }

            if (!existingContests.TryGetValue(actualKey, out var existing)) {
                // Register new participation
                var newParticipation = new ContestParticipation {
                    Collected = contest.Collected,
                    Position = contest.Position ?? -1,
                    MedalEarned = medal,

                    ProfileMemberId = member.Id,
                    ProfileMember = member,
                    JacobContestId = actualKey
                };
                
                newParticipations.Add(newParticipation);
            } else {
                existing.Collected = contest.Collected;
                existing.Position = contest.Position ?? -1;
                existing.MedalEarned = medal;
            }
        }

        context.ContestParticipations.AddRange(newParticipations);
        member.JacobData.Contests.AddRange(newParticipations);

        jacob.Participations = jacob.Contests.Count;
        jacob.EarnedMedals.Gold = jacob.Contests.Count(c => c.MedalEarned == ContestMedal.Gold);
        jacob.EarnedMedals.Silver = jacob.Contests.Count(c => c.MedalEarned == ContestMedal.Silver);
        jacob.EarnedMedals.Bronze = jacob.Contests.Count(c => c.MedalEarned == ContestMedal.Bronze);

        jacob.ContestsLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    
    private static ContestMedal ExtractMedal(this RawJacobContest contest)
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
