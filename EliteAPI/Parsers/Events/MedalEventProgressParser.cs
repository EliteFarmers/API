using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events;

public static class MedalEventProgressParser {
	public static void UpdateMedalProgress(this MedalEventMember eventMember, MedalEvent @event, ProfileMember member) {
		var weights = @event.Data.MedalWeights;

		eventMember.Data = new MedalEventMemberData();
		eventMember.Score = 0;
		
		var start = eventMember.StartTime.ToUnixTimeSeconds();
		var end = eventMember.EndTime.ToUnixTimeSeconds();

		var medals = from participation in member.JacobData.Contests
			where participation.JacobContestId >= start && participation.JacobContestId <= end
			select participation.MedalEarned;
		
		// Count the medals earned by the member in the event
		foreach (var medal in medals) {
			eventMember.Data.ContestParticipations++;
			if (!eventMember.Data.EarnedMedals.TryAdd(medal, 1)) {
				eventMember.Data.EarnedMedals[medal]++;
			}
		}

		foreach (var (medal, count) in eventMember.Data.EarnedMedals) {
			eventMember.Score += weights[medal] * count;
		}
	}
}