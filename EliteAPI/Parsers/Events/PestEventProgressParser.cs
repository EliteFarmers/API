using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events;

public static class PestEventProgressParser {
	public static void UpdateScore(this PestEventMember eventMember, PestEvent @event, ProfileMember? member = null) {
		if (member is not null) {
			// Initialize the start conditions if they haven't been initialized yet
			if (eventMember.Data.InitialPests.Count == 0) {
				eventMember.Initialize(member);
				// Initial run, no need to check for progress
				return;
			}
		}
		else {
			return;
		}

		var initial = eventMember.Data.InitialPests;
		var counted = new Dictionary<Pest, int> {
			{ Pest.Mite, GetIncrease(member.Farming.Pests.Mite, Pest.Mite) },
			{ Pest.Cricket, GetIncrease(member.Farming.Pests.Cricket, Pest.Cricket) },
			{ Pest.Moth, GetIncrease(member.Farming.Pests.Moth, Pest.Moth) },
			{ Pest.Earthworm, GetIncrease(member.Farming.Pests.Earthworm, Pest.Earthworm) },
			{ Pest.Slug, GetIncrease(member.Farming.Pests.Slug, Pest.Slug) },
			{ Pest.Beetle, GetIncrease(member.Farming.Pests.Beetle, Pest.Beetle) },
			{ Pest.Locust, GetIncrease(member.Farming.Pests.Locust, Pest.Locust) },
			{ Pest.Rat, GetIncrease(member.Farming.Pests.Rat, Pest.Rat) },
			{ Pest.Mosquito, GetIncrease(member.Farming.Pests.Mosquito, Pest.Mosquito) },
			{ Pest.Fly, GetIncrease(member.Farming.Pests.Fly, Pest.Fly) },
			{ Pest.Mouse, GetIncrease(member.Farming.Pests.Mouse, Pest.Mouse) }
		};

		var score = counted.Aggregate(0, (acc, pest) => {
			if (pest.Value == 0 || !@event.Data.PestWeights.TryGetValue(pest.Key, out var weight)) return acc;
			return acc + pest.Value * weight;
		});

		// Update the event member and amount gained
		eventMember.Data.CountedPests = counted;
		eventMember.Status = score > eventMember.Score ? EventMemberStatus.Active : EventMemberStatus.Inactive;
		eventMember.Score = score;
		return;

		int GetIncrease(int current, Pest pest) {
			return initial.TryGetValue(pest, out var value)
				? current - value
				: 0;
		}
	}

	public static void Initialize(this PestEventMember eventMember, ProfileMember member) {
		if (eventMember.Data.InitialPests.Count > 0) return;

		eventMember.Data = new PestEventMemberData {
			InitialPests = new Dictionary<Pest, int> {
				{ Pest.Mite, member.Farming.Pests.Mite },
				{ Pest.Cricket, member.Farming.Pests.Cricket },
				{ Pest.Moth, member.Farming.Pests.Moth },
				{ Pest.Earthworm, member.Farming.Pests.Earthworm },
				{ Pest.Slug, member.Farming.Pests.Slug },
				{ Pest.Beetle, member.Farming.Pests.Beetle },
				{ Pest.Locust, member.Farming.Pests.Locust },
				{ Pest.Rat, member.Farming.Pests.Rat },
				{ Pest.Mosquito, member.Farming.Pests.Mosquito },
				{ Pest.Fly, member.Farming.Pests.Fly },
				{ Pest.Mouse, member.Farming.Pests.Mouse }
			},
			CountedPests = new Dictionary<Pest, int>()
		};
	}
}