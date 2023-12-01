using System.Collections.Frozen;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
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
                
                fetched.UpdateMedalBracket(newParticipation);
                newParticipations.Add(newParticipation);
            } else {
                existing.Collected = contest.Collected;
                existing.Position = contest.Position ?? -1;
                existing.MedalEarned = medal;
                
                fetched.UpdateMedalBracket(existing);
            }
        }

        context.ContestParticipations.AddRange(newParticipations);
        member.JacobData.Contests.AddRange(newParticipations);

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
                "platinum" => ContestMedal.Platinum,
                "diamond" => ContestMedal.Diamond,
                _ => ContestMedal.None
            };
        }

        var participants = contest.Participants;
        var position = contest.Position;
        
        if (position is null || participants is null) return ContestMedal.None;

        // Calculate medal based on position
        if (position <= Math.Floor((double) (participants * 0.02))) return ContestMedal.Diamond;
        if (position <= Math.Floor((double) (participants * 0.05))) return ContestMedal.Platinum;
        if (position <= Math.Floor((double) (participants * 0.10))) return ContestMedal.Gold;
        if (position <= Math.Floor((double) (participants * 0.30))) return ContestMedal.Silver;
        if (position <= Math.Floor((double) (participants * 0.60))) return ContestMedal.Bronze;

        return ContestMedal.None;
    }

    public static void CalculateBrackets(this JacobContestWithParticipationsDto contest) {
        var participations = contest.Participations;
        var brackets = contest.Brackets;

        var grouped = participations
            .Where(p => p.Medal != null)
            .OrderBy(p => p.Collected)
            .GroupBy(p => p.Medal)
            .Select(p => new {
                Medal = p.Key!,
                p.First().Collected
            }).ToFrozenDictionary(p => p.Medal, p => p.Collected);
        
        brackets.Bronze = grouped.TryGetValue(ContestMedal.Bronze.MedalName(), out var bronze) ? bronze : -1;
        brackets.Silver = grouped.TryGetValue(ContestMedal.Silver.MedalName(), out var silver) ? silver : -1;
        brackets.Gold = grouped.TryGetValue(ContestMedal.Gold.MedalName(), out var gold) ? gold : -1;
        brackets.Platinum = grouped.TryGetValue(ContestMedal.Platinum.MedalName(), out var platinum) ? platinum : -1;
        brackets.Diamond = grouped.TryGetValue(ContestMedal.Diamond.MedalName(), out var diamond) ? diamond : -1;
    }

    private static void UpdateMedalBracket(this JacobContest contest, ContestParticipation participation) {
        if (participation.MedalEarned == ContestMedal.None) return;

        switch (participation.MedalEarned) {
            case ContestMedal.Bronze:
                if (contest.Bronze == 0 || contest.Bronze > participation.Collected) {
                    contest.Bronze = participation.Collected;
                }
                break;
            case ContestMedal.Silver:
                if (contest.Silver == 0 || contest.Silver > participation.Collected) {
                    contest.Silver = participation.Collected;
                }
                break;
            case ContestMedal.Gold:
                if (contest.Gold == 0 || contest.Gold > participation.Collected) {
                    contest.Gold = participation.Collected;
                }
                break;
            case ContestMedal.Platinum:
                if (contest.Platinum == 0 || contest.Platinum > participation.Collected) {
                    contest.Platinum = participation.Collected;
                }
                break;
            case ContestMedal.Diamond:
                if (contest.Diamond == 0 || contest.Diamond > participation.Collected) {
                    contest.Diamond = participation.Collected;
                }
                break;
        }
    }

    private static string MedalName(this ContestMedal medal) => medal switch {
        ContestMedal.Gold => "gold",
        ContestMedal.Silver => "silver",
        ContestMedal.Bronze => "bronze",
        ContestMedal.Platinum => "platinum",
        ContestMedal.Diamond => "diamond",
        _ => "none"
    };
}
