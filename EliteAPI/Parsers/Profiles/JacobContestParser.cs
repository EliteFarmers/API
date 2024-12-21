using System.Collections.Frozen;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Profiles;

public static class JacobContestParser
{
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

    public static void UpdateMedalBracket(this JacobContest contest, ContestParticipation participation) {
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
    
    public static void AddMedal(this EarnedMedalInventory inventory, ContestParticipation contest) {
        var medal = contest.MedalEarned;
        switch (medal)
        {
            case ContestMedal.Bronze:
                inventory.Bronze++;
                break;
            case ContestMedal.Silver:
                inventory.Silver++;
                break;
            case ContestMedal.Gold:
                inventory.Gold++;
                break;
            case ContestMedal.Platinum:
                inventory.Platinum++;
                break;
            case ContestMedal.Diamond:
                inventory.Diamond++;
                break;
            case ContestMedal.None:
            default:
                break;
        }
        
    }
}
