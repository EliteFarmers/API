using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Profiles;

public static class JacobDataParser
{
    public static void ParseJacob(this ProfileMember member, RawJacobData? incomingJacob)
    {
        var jacob = member.JacobData;

        jacob.EarnedMedals.Gold = 0;
        jacob.EarnedMedals.Silver = 0;
        jacob.EarnedMedals.Bronze = 0;
        jacob.Participations = 0;

        if (incomingJacob is null) return;

        if (incomingJacob.MedalsInventory is not null)
        {
            jacob.Medals.Gold = incomingJacob.MedalsInventory.Gold;
            jacob.Medals.Silver = incomingJacob.MedalsInventory.Silver;
            jacob.Medals.Bronze = incomingJacob.MedalsInventory.Bronze;
        }

        if (incomingJacob.Perks is not null)
        {
            jacob.Perks.DoubleDrops = incomingJacob.Perks.DoubleDrops ?? 0;
            jacob.Perks.LevelCap = incomingJacob.Perks.FarmingLevelCap ?? 0;
        }
    }

    public static async Task ParseJacobContests(this ProfileMember member, RawJacobData? incomingJacob)
    {
        var jacob = member.JacobData;

        if (incomingJacob is null) return;

        var contests = incomingJacob.Contests;

        if (contests is null) return;

        foreach (var contest in contests)
        {
            var contestMedal = ExtractContestMedal(contest);

            if (contestMedal == ContestMedal.None) continue;

            jacob.Participations++;
            switch (contestMedal)
            {
                case ContestMedal.Gold:
                    jacob.EarnedMedals.Gold++;
                    break;
                case ContestMedal.Silver:
                    jacob.EarnedMedals.Silver++;
                    break;
                case ContestMedal.Bronze:
                    jacob.EarnedMedals.Bronze++;
                    break;
            }
        }

        jacob.EarnedMedals.Total = jacob.EarnedMedals.Gold + jacob.EarnedMedals.Silver + jacob.EarnedMedals.Bronze;
    }

    public static ContestMedal ExtractContestMedal(RawJacobContest contest)
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
