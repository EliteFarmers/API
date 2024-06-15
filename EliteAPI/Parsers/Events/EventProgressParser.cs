using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events; 

public static class EventProgressParser {
    
    public static bool IsEventRunning(this EventMember member) {
        var currentTime = DateTimeOffset.UtcNow;
        
        // Check if event is running
        return member.StartTime < currentTime && member.EndTime > currentTime;
    }
    
    /// <summary>
    /// Determines if the event is a custom team event
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public static bool IsCustomTeamEvent(this Event @event) {
        return @event.MaxTeamMembers != 0;
    }
    
    public static void LoadProgress(this EventMember eventMember, ProfileMember member, Event @event) {
        var currentTime = DateTimeOffset.UtcNow;

        // Skip if the member is already disqualified or the event hasn't started yet
        if (eventMember.Status == EventMemberStatus.Disqualified) return;
        if (!eventMember.IsEventRunning()) {
            eventMember.Status = EventMemberStatus.Inactive;
            return;
        }

        eventMember.LastUpdated = currentTime;

        // Disqualify the member if they disabled API access during the event
        if (!member.Api.Collections || !member.Api.Inventories) {
            eventMember.Status = EventMemberStatus.Disqualified;
            eventMember.Notes = "API access was disabled during the event.";
            return;
        }
        
        // Update the tool states and collection increases
        // eventMember.UpdateToolsAndCollections(member);

        switch (@event.Type) {
            case EventType.None:
            case EventType.FarmingWeight:
                if (eventMember is WeightEventMember m && @event is WeightEvent weightEvent) {
                    m.UpdateFarmingWeight(weightEvent, member);
                }
                break;
            case EventType.Collection:
                break;
            case EventType.Experience:
                break;
            case EventType.Medals:
                if (eventMember is MedalEventMember medalMember && @event is MedalEvent medalEvent) {
                    medalMember.UpdateMedalProgress(medalEvent, member);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}