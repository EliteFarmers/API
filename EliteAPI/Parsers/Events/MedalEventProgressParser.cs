using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events;

public static class MedalEventProgressParser {
	public static void UpdateMedalProgress(this MedalEventMember eventMember, MedalEvent @event, ProfileMember member) {
		var weights = @event.Data.MedalWeights;

		eventMember.Data = new MedalEventMemberData();
		
		var start = eventMember.StartTime.ToUnixTimeSeconds();
		var end = eventMember.EndTime.ToUnixTimeSeconds();

		var medals = from participation in member.JacobData.Contests
			where participation.JacobContestId >= start && participation.JacobContestId <= end
			select participation.MedalEarned;
		
		// Count the medals earned by the member in the event
		var newScore = 0.0;
		foreach (var medal in medals) {
			eventMember.Data.ContestParticipations++;
			if (!weights.ContainsKey(medal)) continue;
			
			newScore += weights[medal];

			if (!eventMember.Data.EarnedMedals.TryAdd(medal, 1)) {
				eventMember.Data.EarnedMedals[medal]++;
			}
		}
		
		// Update the event member status and amount gained
		eventMember.Status = newScore > eventMember.Score ? EventMemberStatus.Active : EventMemberStatus.Inactive;
		eventMember.Score = newScore;
	}
}