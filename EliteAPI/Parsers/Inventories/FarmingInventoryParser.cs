using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Inventories;

public static class FarmingInventoryParser {
	public static async Task<FarmingInventory> ExtractFarmingItems(this ProfileMemberResponse memberData,
		ProfileMember member) {
		var farming = new FarmingInventory();
		var incoming = memberData.Inventories;

		if (incoming is null) {
			member.Api.Inventories = false;
			member.Api.Vault = false;
			return farming;
		}

		member.Api.Inventories = incoming.InventoryContents is not null;
		member.Api.Vault = incoming.PersonalVaultContents is not null;

		if (incoming.InventoryContents is null) return farming;

		var tasks = new List<Task> {
			farming.PopulateFrom(incoming.InventoryContents?.Data),
			farming.PopulateFrom(incoming.EnderChestContents?.Data),
			farming.PopulateFrom(incoming.PersonalVaultContents?.Data),
			farming.PopulateFrom(incoming.Armor?.Data),
			farming.PopulateFrom(incoming.WardrobeContents?.Data),
			farming.PopulateFrom(incoming.EquipmentContents?.Data),
			farming.PopulateFrom(incoming.BagContents?.TalismanBag?.Data)
		};
		tasks.AddRange(incoming.BackpackContents?.Values
			.Select(i => farming.PopulateFrom(i.Data)) ?? new List<Task>());

		await Task.WhenAll(tasks);

		return farming;
	}

	private static async Task PopulateFrom(this FarmingInventory farming, string? inventory) {
		var data = await NbtParser.NbtToItems(inventory);
		if (data is null || data.Count == 0) return;

		var toolIds = FarmingItemsConfig.Settings.FarmingToolIds;
		var equipmentIds = FarmingItemsConfig.Settings.FarmingEquipmentIds;
		var armorIds = FarmingItemsConfig.Settings.FarmingArmorIds;
		var accessoryIds = FarmingItemsConfig.Settings.FarmingAccessoryIds;

		foreach (var item in data) {
			if (item?.SkyblockId is null) continue;

			if (toolIds.ContainsKey(item.SkyblockId)) farming.Tools.Add(item);

			if (equipmentIds.ContainsKey(item.SkyblockId)) farming.Equipment.Add(item);

			if (armorIds.ContainsKey(item.SkyblockId)) farming.Armor.Add(item);

			if (accessoryIds.ContainsKey(item.SkyblockId)) farming.Accessories.Add(item);
		}
	}
}