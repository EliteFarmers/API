using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events;

public static class MedalEventProgressParser {
	public static void UpdateMedalProgress(this MedalEventMember eventMember, MedalEvent @event, ProfileMember member) {
		var weights = @event.Data.MedalWeights;

		eventMember.Data = new MedalEventMemberData();
		
		var start = eventMember.StartTime.ToUnixTimeSeconds();
		var end = eventMember.EndTime.ToUnixTimeSeconds();

		var participations = from participation in member.JacobData.Contests
			where participation.JacobContestId >= start && participation.JacobContestId <= end
			select participation;
		
		// Count the medals earned by the member in the event
		var newScore = 0.0;
		foreach (var participation in participations) {
			eventMember.Data.ContestParticipations++;
			var medal = participation.MedalEarned;
			
			if (participation.MedalEarned == ContestMedal.None) {
				// No medal claimed, estimate the medal based the Jacob Contests brackets
				var contest = participation.JacobContest;
				
				if (participation.Collected >= contest.Diamond && contest.Diamond > 0) {
					medal = ContestMedal.Diamond;
				} else if (participation.Collected >= contest.Gold && contest.Gold > 0) {
					medal = ContestMedal.Gold;
				} else if (participation.Collected >= contest.Silver && contest.Silver > 0) {
					medal = ContestMedal.Silver;
				} else if (participation.Collected >= contest.Bronze && contest.Bronze > 0) {
					medal = ContestMedal.Bronze;
				} else {
					medal = ContestMedal.None;
				}
			}
			
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