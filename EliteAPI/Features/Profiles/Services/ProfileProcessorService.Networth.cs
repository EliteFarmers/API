using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.Entities.Hypixel;
using HypixelAPI.Networth.Models;

namespace EliteAPI.Features.Profiles.Services;

// Networth calculation methods for ProfileProcessorService
public partial class ProfileProcessorService
{
	public async Task<NetworthBreakdown> GetNetworthBreakdownAsync(ProfileMember member) {
		var prices = await _priceProvider.GetPricesAsync();
		var breakdown = new NetworthBreakdown();

		// Bank and Purse
		breakdown.Purse = member.Purse;
		breakdown.Networth += breakdown.Purse;
		breakdown.UnsoulboundNetworth += breakdown.Purse;

		if (member.Profile?.BankBalance > 0) {
			breakdown.Bank = member.Profile.BankBalance;
			breakdown.Networth += breakdown.Bank;
			breakdown.UnsoulboundNetworth += breakdown.Bank;
		}

		if (member.PersonalBank > 0) {
			breakdown.PersonalBank = member.PersonalBank;
			breakdown.Networth += breakdown.PersonalBank;
			breakdown.UnsoulboundNetworth += breakdown.PersonalBank;
		}

		// Inventories
		foreach (var inventory in member.Inventories) {
			var categoryName = inventory.Name;
			if (!breakdown.Categories.ContainsKey(categoryName)) {
				breakdown.Categories[categoryName] = new NetworthCategory();
			}

			var category = breakdown.Categories[categoryName];

			foreach (var item in inventory.Items) {
				var networthItem = item.ToNetworthItem();
				var result = await _networthCalculator.CalculateAsync(networthItem, prices);

				category.Total += result.Price;
				category.Items.Add(result);

				breakdown.Networth += result.Price;
				breakdown.UnsoulboundNetworth += result.LiquidNetworth;
			}
		}

		// Sacks
		var sacksCategory = new NetworthCategory();
		foreach (var (id, count) in member.Sacks) {
			if (prices.TryGetValue(id, out var price)) {
				var total = price * count;
				sacksCategory.Total += total;
				sacksCategory.UnsoulboundTotal += total;

				sacksCategory.Items.Add(new NetworthResult {
					Price = total,
					BasePrice = price,
					Item = new NetworthItem { SkyblockId = id, Count = (int)count, Name = id }
				});
			}
		}

		breakdown.Categories["sacks"] = sacksCategory;
		breakdown.Networth += sacksCategory.Total;
		breakdown.UnsoulboundNetworth += sacksCategory.UnsoulboundTotal;

		// Essence
		var essenceCategory = new NetworthCategory();
		if (member.Unparsed?.Essence != null) {
			foreach (var (type, amount) in member.Unparsed.Essence) {
				var essenceId = $"ESSENCE_{type}";
				if (prices.TryGetValue(essenceId, out var price)) {
					var total = price * amount;
					essenceCategory.Total += total;
					essenceCategory.UnsoulboundTotal += total;

					essenceCategory.Items.Add(new NetworthResult {
						Price = total,
						BasePrice = price,
						Item = new NetworthItem { SkyblockId = essenceId, Count = amount, Name = type }
					});
				}
			}
		}

		breakdown.Categories["essence"] = essenceCategory;
		breakdown.Networth += essenceCategory.Total;
		breakdown.UnsoulboundNetworth += essenceCategory.UnsoulboundTotal;

		// Pets
		var petsCategory = new NetworthCategory();
		foreach (var pet in member.Pets) {
			var networthItem = new NetworthItem {
				PetInfo = new NetworthItemPetInfo {
					Type = pet.Type ?? "UNKNOWN",
					Tier = pet.Tier ?? "COMMON",
					Level = pet.Level,
					Exp = (decimal)pet.Exp,
					Skin = pet.Skin,
					HeldItem = pet.HeldItem,
					CandyUsed = pet.CandyUsed,
					Active = pet.Active
				}
			};

			var result = await _petNetworthCalculator.CalculateAsync(networthItem, prices);
			petsCategory.Total += result.Price;
			petsCategory.Items.Add(result);
		}

		breakdown.Categories["pets"] = petsCategory;
		breakdown.Networth += petsCategory.Total;
		breakdown.UnsoulboundNetworth += petsCategory.Total;

		return breakdown;
	}
}