using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Profiles;

public static class ChocolateFactoryParser
{
	public static void ParseChocolateFactory(this ProfileMember member, EasterEventDataResponse incoming,
		ChocolateFactorySettings settings) {
		member.ChocolateFactory ??= new ChocolateFactory();

		var factory = member.ChocolateFactory;
		factory.ProfileMemberId = member.Id;
		factory.ProfileMember = member;

		factory.Chocolate = incoming.Chocolate;
		factory.TotalChocolate = incoming.TotalChocolate;
		factory.ChocolateSincePrestige = incoming.ChocolateSincePrestige;
		factory.ChocolateSpent = incoming.Shop.ChocolateSpent;
		factory.CocoaFortuneUpgrades = incoming.Shop.CocoaFortuneUpgrades;
		factory.RefinedTrufflesConsumed = incoming.RefinedDarkCacaoTrufflesConsumed;

		factory.Prestige = incoming.Prestige;
		factory.LastViewedChocolateFactory = incoming.LastViewedChocolateFactory;

		(factory.UniqueRabbits.Common, factory.TotalRabbits.Common) =
			CountRabbits(incoming, settings.Rabbits.Common.Rabbits);
		(factory.UniqueRabbits.Uncommon, factory.TotalRabbits.Uncommon) =
			CountRabbits(incoming, settings.Rabbits.Uncommon.Rabbits);
		(factory.UniqueRabbits.Rare, factory.TotalRabbits.Rare) = CountRabbits(incoming, settings.Rabbits.Rare.Rabbits);
		(factory.UniqueRabbits.Epic, factory.TotalRabbits.Epic) = CountRabbits(incoming, settings.Rabbits.Epic.Rabbits);
		(factory.UniqueRabbits.Legendary, factory.TotalRabbits.Legendary) =
			CountRabbits(incoming, settings.Rabbits.Legendary.Rabbits);
		(factory.UniqueRabbits.Mythic, factory.TotalRabbits.Mythic) =
			CountRabbits(incoming, settings.Rabbits.Mythic.Rabbits);
		(factory.UniqueRabbits.Divine, factory.TotalRabbits.Divine) =
			CountRabbits(incoming, settings.Rabbits.Divine.Rabbits);

		if (!factory.UnlockedZorro) factory.UnlockedZorro = incoming.Rabbits.ContainsKey("zorro");
	}

	private static (int uniques, int total) CountRabbits(EasterEventDataResponse incoming, List<string> rabbits) {
		var uniques = 0;
		var total = 0;

		foreach (var rabbit in rabbits) {
			if (!incoming.Rabbits.TryGetValue(rabbit, out var incomingRabbit)) continue;

			uniques++;
			total += incomingRabbit;
		}

		return (uniques, total);
	}
}