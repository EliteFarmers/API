using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.Entities.Hypixel;
using HypixelAPI.Networth.Models;

namespace EliteAPI.Features.Profiles.Services;

// Networth calculation methods for ProfileProcessorService
public partial class ProfileProcessorService
{
	public async Task<NetworthBreakdown> GetNetworthBreakdownAsync(ProfileMember member) {
		var prices = await priceProvider.GetPricesAsync();
		var breakdown = new NetworthBreakdown();

		// Bank and Purse
		breakdown.Purse = member.Purse;
		breakdown.Networth += breakdown.Purse;
		breakdown.LiquidNetworth += breakdown.Purse;
		breakdown.FunctionalNetworth += breakdown.Purse;
		breakdown.LiquidFunctionalNetworth += breakdown.Purse;

		if (member.Profile?.BankBalance > 0) {
			breakdown.Bank = member.Profile.BankBalance;
			breakdown.Networth += breakdown.Bank;
			breakdown.LiquidNetworth += breakdown.Bank;
			breakdown.FunctionalNetworth += breakdown.Bank;
			breakdown.LiquidFunctionalNetworth += breakdown.Bank;
		}

		if (member.PersonalBank > 0) {
			breakdown.PersonalBank = member.PersonalBank;
			breakdown.Networth += breakdown.PersonalBank;
			breakdown.LiquidNetworth += breakdown.PersonalBank;
			breakdown.FunctionalNetworth += breakdown.PersonalBank;
			breakdown.LiquidFunctionalNetworth += breakdown.PersonalBank;
		}

		// Inventories
		foreach (var inventory in member.Inventories) {
			var categoryName = inventory.Name;
			if (!breakdown.Categories.ContainsKey(categoryName)) {
				breakdown.Categories[categoryName] = new NetworthCategory();
			}

			var category = breakdown.Categories[categoryName];

			foreach (var item in inventory.Items) {
				if (categoryName == "museum" && item.Attributes?.Extra.ContainsKey("museum_borrowing") is true) continue;
				var networthItem = item.ToNetworthItem();
				var result = await networthCalculator.CalculateAsync(networthItem, prices);

				category.Total += result.Price;
				category.LiquidTotal += result.LiquidNetworth;
				category.FunctionalTotal += result.FunctionalNetworth;
				category.LiquidFunctionalTotal += result.LiquidFunctionalNetworth;
				category.Items.Add(result);

				breakdown.Networth += result.Price;
				breakdown.LiquidNetworth += result.LiquidNetworth;
				breakdown.FunctionalNetworth += result.FunctionalNetworth;
				breakdown.LiquidFunctionalNetworth += result.LiquidFunctionalNetworth;
			}
		}

		// Sacks
		var sacksCategory = new NetworthCategory();
		foreach (var (id, count) in member.Sacks) {
			if (prices.TryGetValue(id, out var price)) {
				var total = price * count;
				sacksCategory.Total += total;
				sacksCategory.LiquidTotal += total;
				sacksCategory.FunctionalTotal += total;
				sacksCategory.LiquidFunctionalTotal += total;

				sacksCategory.Items.Add(new NetworthResult {
					Price = total,
					BasePrice = price,
					Item = new NetworthItemSimple { SkyblockId = id, Count = (int)count, Name = id },
					Networth = total,
					LiquidNetworth = total,
					FunctionalNetworth = total,
					LiquidFunctionalNetworth = total
				});
			}
		}

		breakdown.Categories["sacks"] = sacksCategory;
		breakdown.Networth += sacksCategory.Total;
		breakdown.LiquidNetworth += sacksCategory.LiquidTotal;
		breakdown.FunctionalNetworth += sacksCategory.FunctionalTotal;
		breakdown.LiquidFunctionalNetworth += sacksCategory.LiquidFunctionalTotal;

		// Essence
		var essenceCategory = new NetworthCategory();
		if (member.Unparsed?.Essence != null) {
			foreach (var (type, amount) in member.Unparsed.Essence) {
				var essenceId = $"ESSENCE_{type}";
				if (prices.TryGetValue(essenceId, out var price)) {
					var total = price * amount;
					essenceCategory.Total += total;
					essenceCategory.LiquidTotal += total;
					essenceCategory.FunctionalTotal += total;
					essenceCategory.LiquidFunctionalTotal += total;

					essenceCategory.Items.Add(new NetworthResult {
						Price = total,
						BasePrice = price,
						Item = new NetworthItemSimple { SkyblockId = essenceId, Count = amount, Name = type },
						Networth = total,
						LiquidNetworth = total,
						FunctionalNetworth = total,
						LiquidFunctionalNetworth = total
					});
				}
			}
		}

		breakdown.Categories["essence"] = essenceCategory;
		breakdown.Networth += essenceCategory.Total;
		breakdown.LiquidNetworth += essenceCategory.LiquidTotal;
		breakdown.FunctionalNetworth += essenceCategory.FunctionalTotal;
		breakdown.LiquidFunctionalNetworth += essenceCategory.LiquidFunctionalTotal;

		// Pets
		var petsCategory = new NetworthCategory();
		foreach (var pet in member.Pets) {
			var networthItem = new NetworthItem {
				Count = 1,
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

			var result = await petNetworthCalculator.CalculateAsync(networthItem, prices);
			petsCategory.Total += result.Price;
			petsCategory.LiquidTotal += result.LiquidNetworth;
			petsCategory.FunctionalTotal += result.FunctionalNetworth;
			petsCategory.LiquidFunctionalTotal += result.LiquidFunctionalNetworth;
			petsCategory.Items.Add(result);
		}

		breakdown.Categories["pets"] = petsCategory;
		breakdown.Networth += petsCategory.Total;
		breakdown.LiquidNetworth += petsCategory.LiquidTotal;
		breakdown.FunctionalNetworth += petsCategory.FunctionalTotal;
		breakdown.LiquidFunctionalNetworth += petsCategory.LiquidFunctionalTotal;

		return breakdown;
	}
}