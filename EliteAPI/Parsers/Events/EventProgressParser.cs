using EliteAPI.Data;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

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
    
    public static void LoadProgress(this EventMember eventMember, DataContext context, ProfileMember member, Event @event) {
        var currentTime = DateTimeOffset.UtcNow;

        // Skip if the member is already disqualified or the event hasn't started yet
        if (eventMember.Status == EventMemberStatus.Disqualified) return;
        if (!eventMember.IsEventRunning()) {
            eventMember.Status = EventMemberStatus.Inactive;
            return;
        }
        
        // Update estimated time active
        // This isn't perfect as it uses the status from the last update
        eventMember.UpdateEstimatedTimeActive();
        eventMember.LastUpdated = currentTime;

        switch (@event.Type) {
            case EventType.None:
            case EventType.FarmingWeight:
                if (eventMember is WeightEventMember m && @event is WeightEvent weightEvent) {
                    if (!member.Api.Collections || !member.Api.Inventories) {
                        ApiAccessDisqualifyMember();
                        return;
                    }
                    
                    m.UpdateFarmingWeight(weightEvent, member);
                }
                break;
            case EventType.Collection:
                if (eventMember is CollectionEventMember collectionMember && @event is CollectionEvent collectionEvent) {
                    if (!member.Api.Collections) {
                        ApiAccessDisqualifyMember();
                        return;
                    }
                    
                    collectionMember.UpdateScore(collectionEvent, member);
                }
                break;
            case EventType.Experience:
                break;
            case EventType.Medals:
                if (eventMember is MedalEventMember medalMember && @event is MedalEvent medalEvent) {
                    if (!member.Api.Collections) {
                        ApiAccessDisqualifyMember();
                        return;
                    }
                    
                    medalMember.UpdateMedalProgress(medalEvent, member);
                }
                break;
            case EventType.Pests:
                if (!member.Api.Collections) {
                    ApiAccessDisqualifyMember();
                    return;
                }
                
                if (eventMember is PestEventMember pestsMember && @event is PestEvent pestsEvent) {
                    pestsMember.UpdateScore(pestsEvent, member);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        context.Entry(eventMember).State = EntityState.Modified;
        return;
        
        void ApiAccessDisqualifyMember() {
            eventMember.Status = EventMemberStatus.Disqualified;
            eventMember.Notes = "API access was disabled during the event.";
            
            if (eventMember.Team is not null) {
                eventMember.Team = null;
                eventMember.TeamId = null;
                
                eventMember.Team?.Members.Remove(eventMember);
                if (eventMember.Team?.Members.Count == 0) {
                    context.Entry(eventMember.Team).State = EntityState.Deleted;
                }
            }
        }
    }
    
    private static void UpdateEstimatedTimeActive(this EventMember eventMember) {
        if (eventMember.Status != EventMemberStatus.Active) return;
        
        // Add difference in seconds to estimated time active
        eventMember.EstimatedTimeActive += (long)(DateTimeOffset.UtcNow - eventMember.LastUpdated).TotalSeconds;
    }
}