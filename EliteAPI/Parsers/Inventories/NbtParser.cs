using System.Globalization;
using System.Text.Json;
using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using MinecraftRenderer;
using MinecraftRenderer.Nbt;
using SkyblockRepo;

namespace EliteAPI.Parsers.Inventories;

/// <summary>
/// Refactored NBT parser using MinecraftRenderer's NBT library.
/// </summary>
public static class NbtParser
{
	private static MinecraftBlockRenderer? _cachedRenderer;

	/// <summary>
	/// Set the renderer instance to use for computing resource IDs.
	/// Call this once at startup with your configured renderer.
	/// </summary>
	public static void SetRenderer(MinecraftBlockRenderer renderer) {
		_cachedRenderer = renderer;
	}

	/// <summary>
	/// Decode base64-encoded, gzipped NBT data.
	/// </summary>
	public static NbtDocument? DecodeNbt(string? data) {
		if (string.IsNullOrEmpty(data)) return null;

		try {
			var decodedBytes = Convert.FromBase64String(data);

			// Use MinecraftRenderer's NbtParser which handles GZip automatically
			return MinecraftRenderer.Nbt.NbtParser.ParseBinary(decodedBytes);
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return null;
		}
	}

	public static HypixelInventory? ParseInventory(string inventoryName, string? data) {
		var list = InventoryDataToNbtList(data);
		if (list is null || list.Count == 0) return null;

		var items = new List<ItemDto>(list.Count);
		var empty = new List<short>();

		short i = -1;
		foreach (var item in list) {
			i++;
			if (item is not NbtCompound compound) {
				empty.Add(i);
				continue;
			}

			var parsedItem = ToItem(compound);
			if (parsedItem?.SkyblockId is null) {
				empty.Add(i);
				continue;
			}

			parsedItem.Slot = i.ToString();
			items.Add(parsedItem);
		}

		return new HypixelInventory {
			Name = inventoryName,
			Hash = HashUtility.ComputeSha256Hash(data ?? string.Empty),
			Items = items.Select(item => item.ToHypixelItem()).ToList(),
			EmptySlots = empty.Count == 0 ? null : empty.ToArray()
		};
	}

	/// <summary>
	/// Parse NBT data into a list of ItemDto objects
	/// </summary>
	public static List<ItemDto?> NbtToItems(string? data) {
		var list = InventoryDataToNbtList(data);
		if (list is null || list.Count == 0) return [];

		var items = new List<ItemDto?>(list.Count);

		var i = -1;
		foreach (var item in list) {
			i++;
			if (item is not NbtCompound compound) continue;
			var parsedItem = ToItem(compound);
			if (parsedItem?.SkyblockId is null) continue;
			parsedItem.Slot = i.ToString();
			items.Add(parsedItem);
		}

		return items;
	}
	
	/// <summary>
	/// Parse NBT data into a list of ItemDto objects
	/// </summary>
	public static Dictionary<string, ItemDto?> NbtToItemDictionary(string? data) {
		var list = InventoryDataToNbtList(data);
		if (list is null || list.Count == 0) return [];

		var items = new Dictionary<string, ItemDto?>();
		
		short i = -1;
		foreach (var item in list) {
			i++;
			if (item is not NbtCompound compound) {
				items.TryAdd(i.ToString(), null);
				continue;
			}

			var parsedItem = ToItem(compound);
			if (parsedItem?.SkyblockId is null) {
				items.TryAdd(i.ToString(), null);
				continue;
			}

			parsedItem.Slot = i.ToString();
			items.Add(parsedItem.Slot, parsedItem);
		}

		return items;
	}

	private static NbtList? InventoryDataToNbtList(string? data) {
		if (string.IsNullOrEmpty(data)) return null;

		var nbt = DecodeNbt(data);
		if (nbt?.RootCompound is null) return null;

		// Try to find the inventory list in the root compound
		// Common keys: "i", "items", "inventory", "data"
		NbtList? list = null;
		foreach (var key in new[] { "i", "items", "inventory", "data" }) {
			if (nbt.RootCompound.TryGetValue(key, out var tag) && tag is NbtList foundList) {
				list = foundList;
				break;
			}
		}

		return list ?? null;
	}

	/// <summary>
	/// Parse NBT data into a single ItemDto object with texture ID.
	/// </summary>
	public static ItemDto? NbtToItem(string? itemData) {
		if (string.IsNullOrEmpty(itemData)) return null;

		var nbt = DecodeNbt(itemData);
		if (nbt?.RootCompound is null) return null;

		// Try to find first item in the list
		NbtList? list = null;
		foreach (var key in new[] { "i", "items", "inventory", "data" }) {
			if (!nbt.RootCompound.TryGetValue(key, out var tag) || tag is not NbtList foundList) continue;
			list = foundList;
			break;
		}

		if (list is null || list.Count == 0) return null;
		return list[0] is not NbtCompound compound ? null : ToItem(compound);
	}

	/// <summary>
	/// Convert an NBT compound tag to an ItemDto, including resource ID generation.
	/// </summary>
	public static ItemDto? ToItem(NbtCompound tag) {
		// Extract basic item info
		var itemId = tag.GetShort("id") ?? 0;
		var damage = tag.GetShort("Damage") ?? 0;
		var count = tag.GetByte("Count") ?? 0;

		// Get tag compound
		var tagCompound = tag.GetCompound("tag");
		if (tagCompound is null) return null;

		// Get ExtraAttributes (Hypixel data)
		var extraAttributes = tagCompound.GetCompound("ExtraAttributes");
		var skyblockId = extraAttributes?.GetString("id");
		var petInfo = extraAttributes?.GetString("petInfo");
		
		// Skull texture
		var skullOwner = tagCompound.GetCompound("SkullOwner");
		string? skullValue = null;
		if (skullOwner is not null) {
			var properties = skullOwner.GetCompound("Properties");
			var texturesList = properties?.GetList("textures");
			var firstTexture = texturesList is { Count: > 0 } && texturesList[0] is NbtCompound texComp
				? texComp
				: null;
			skullValue = firstTexture?.GetString("Value") ?? null;
		}

		// Extract gems
		var gemsCompound = extraAttributes?.GetCompound("gems");
		var unlockedGems = gemsCompound?.GetList("unlocked_slots")?
			.Where(g => g is NbtString)
			.Select(g => ((NbtString)g).Value)
			.ToList();

		var gems = new Dictionary<string, string?>();
		if (gemsCompound != null) {
			foreach (var kvp in gemsCompound) {
				if (string.IsNullOrEmpty(kvp.Key) || kvp.Key == "unlocked_slots") continue;

				if (kvp.Value is NbtString gemString) {
					gems[kvp.Key] = gemString.Value;
				}
				else if (kvp.Value is NbtCompound gemCompound) {
					var quality = gemCompound.GetString("quality");
					if (!string.IsNullOrEmpty(quality)) {
						gems[kvp.Key] = quality;
					}
				}
			}
		}

		// Add unlocked but empty gem slots
		if (unlockedGems != null) {
			foreach (var gem in unlockedGems) {
				gems.TryAdd(gem, null);
			}
		}

		// Extract display info
		var display = tagCompound.GetCompound("display");
		var displayName = display?.GetString("Name");
		var loreList = display?.GetList("Lore")?
			.Where(l => l is NbtString)
			.Select(l => ((NbtString)l).Value)
			.ToList();

		// Extract enchantments
		var enchantments = extraAttributes?.GetCompound("enchantments")?
			.Where(kvp => !string.IsNullOrEmpty(kvp.Key))
			.Select(kvp => new KeyValuePair<string, int>(
				kvp.Key,
				kvp.Value switch {
					NbtInt intTag => intTag.Value,
					NbtShort shortTag => shortTag.Value,
					NbtByte byteTag => byteTag.Value,
					_ => 0
				}))
			.Where(kvp => kvp.Value > 0)
			.ToDictionary(x => x.Key, x => x.Value);

		// Extract general attributes (from ExtraAttributes root)
		var attributes = extraAttributes?
			.Where(kvp => !string.IsNullOrEmpty(kvp.Key) &&
			              kvp.Key != "id" &&
			              kvp.Key != "uuid" &&
			              // kvp.Key != "petInfo" &&
			              kvp.Key != "enchantments" &&
			              kvp.Key != "gems" &&
			              kvp.Key != "attributes" &&
			              IsSimpleType(kvp.Value))
			.Select(kvp => new KeyValuePair<string, string>(
				kvp.Key,
				GetValueAsString(kvp.Value) ?? string.Empty))
			.ToDictionary(x => x.Key, x => x.Value);
		
		var compoundAttributes = extraAttributes?
			.Where(kvp => !string.IsNullOrEmpty(kvp.Key) &&
			              kvp.Key != "id" &&
			              kvp.Key != "uuid" &&
			              kvp.Key != "petInfo" &&
			              kvp.Key != "enchantments" &&
			              kvp.Key != "gems" &&
			              kvp.Key != "attributes" &&
			              !IsSimpleType(kvp.Value))
			.Select(kvp => new KeyValuePair<string, NbtTag>(kvp.Key, kvp.Value))
			.ToDictionary(x => x.Key, x => x.Value);
		
		// Extract item attributes (Kuudra armor, etc.)
		var itemAttributes = extraAttributes?.GetCompound("attributes")?
			.Where(kvp => !string.IsNullOrEmpty(kvp.Key) && IsSimpleType(kvp.Value))
			.Select(kvp => new KeyValuePair<string, string>(
				kvp.Key,
				GetValueAsString(kvp.Value) ?? string.Empty))
			.ToDictionary(x => x.Key, x => x.Value);

		// Create ItemDto
		var item = new ItemDto {
			Id = itemId,
			Count = count,
			Damage = damage,
			SkyblockId = skyblockId,
			Uuid = extraAttributes?.GetString("uuid"),
			Name = displayName,
			Lore = loreList,
			Enchantments = enchantments,
			Attributes = attributes is null ? attributes : null,
			ItemAttributes = itemAttributes,
			Gems = gems.Count > 0 ? gems : null,
		};
		
		// Check if we need to save this skull texture
		if (skullValue is not null && skyblockId is not null) {
			SkyblockRepoClient.Data.Items.TryGetValue(skyblockId, out var repoData);
			var skin = repoData?.Data?.Skin?.Value;
			if (skin is null || skin != skullValue) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Extra.TryAdd("skin_texture", skullValue);
			}
		}
		
		if (compoundAttributes is { Count: > 0 }) {
			if (compoundAttributes.TryGetValue("runes", out var runes) && runes is NbtCompound runesCompound) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Runes = runesCompound
					.Where(kvp => IsIntType(kvp.Value))
					.ToDictionary(k => k.Key, v => GetValueAsInt(v.Value) ?? 1);
			}
			
			if (compoundAttributes.TryGetValue("effects", out var effects) && effects is NbtList effectsList) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Effects = effectsList
					.Where(entry => entry is NbtCompound)
					.Select(entry => {
						var effect = (NbtCompound)entry;
						return new ItemEffectAttribute() {
							Level = effect.TryGetValue("level", out var level) ? GetValueAsInt(level) ?? 0 : 0,
							Effect = effect.TryGetValue("effect", out var name) ? GetValueAsString(name) ?? string.Empty : string.Empty,
							DurationTicks = effect.TryGetValue("duration_ticks", out var ticks) ? GetValueAsInt(ticks) ?? 0 : 0,
						};
					}).ToList();
			}
			
			if (compoundAttributes.TryGetValue("necromancer_souls", out var souls) && souls is NbtList soulsList) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.NecromancerSouls = soulsList
					.Where(entry => entry is NbtCompound)
					.Select(entry => {
						var effect = (NbtCompound)entry;
						return new ItemSoulAttribute() {
							MobId = effect.TryGetValue("mob_id", out var mob) ? GetValueAsString(mob) : null,
							DroppedModeId = effect.TryGetValue("dropped_mode_id", out var mode) ? GetValueAsString(mode) : null,
							DroppedInstanceId = effect.TryGetValue("dropped_instance_id", out var instance) ? GetValueAsString(instance) : null,
						};
					}).ToList();
			}

			if (compoundAttributes.TryGetValue("hook", out var hook) && hook is NbtCompound hookCompound) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Hook = new ItemRodPartAttribute() {
					Part = hookCompound.TryGetValue("part", out var part) ? GetValueAsString(part) : null,
				};
			}
			
			if (compoundAttributes.TryGetValue("line", out var line) && line is NbtCompound lineCompound) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Line = new ItemRodPartAttribute() {
					Part = lineCompound.TryGetValue("part", out var part) ? GetValueAsString(part) : null,
				};
			}
			
			if (compoundAttributes.TryGetValue("sinker", out var sinker) && sinker is NbtCompound sinkerCompound) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Sinker = new ItemRodPartAttribute() {
					Part = sinkerCompound.TryGetValue("part", out var part) ? GetValueAsString(part) : null,
				};
			}
			
			if (compoundAttributes.TryGetValue("ability_scroll", out var scrolls) && scrolls is NbtList scrollsList) {
				item.Attributes ??= new ItemAttributes();
				item.Attributes.AbilityScrolls = scrollsList
					.Select(s => GetValueAsString(s) ?? string.Empty)
					.Where(s => !s.IsNullOrEmpty())
					.ToList();
			}
		}

		var inventoryAttributeValue = extraAttributes?
			.Where(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Key.EndsWith("_data") && kvp.Value is NbtByteArray)
			.Select(kvp => (NbtByteArray)kvp.Value)
			.FirstOrDefault();
		
		if (inventoryAttributeValue is not null) {
			try {
				var stringValue = Convert.ToBase64String(inventoryAttributeValue.Values);
				var items = NbtToItemDictionary(stringValue);
				item.Attributes ??= new ItemAttributes();
				item.Attributes.Inventory = items;
			}
			catch {
				// ignored
			}
		}

		// Parse pet info if present
		if (!string.IsNullOrEmpty(petInfo)) {
			try {
				var info = JsonSerializer.Deserialize<ItemPetInfoDto>(petInfo);
				if (info is not null) {
					info.Level = info.GetLevel();
					item.PetInfo = info;
				}
			}
			catch {
				// Ignored
			}
		}

		return item;
	}

	/// <summary>
	/// Convert MinecraftRenderer NBT tag to a dictionary representation.
	/// </summary>
	public static Dictionary<string, object?> ToDictionary(NbtTag tag) {
		var dict = new Dictionary<string, object?> {
			{ "type", tag.Type }
		};

		switch (tag) {
			case NbtList list:
				if (list.Count == 0) {
					dict.Add("value", new List<object>());
				}
				else {
					// Check if all items are the same type
					var firstType = list[0].Type;
					var allSameType = list.All(t => t.Type == firstType);

					dict.Add("value",
						allSameType && firstType != NbtTagType.Compound
							? list.Select(GetValueAsObject).ToList()
							: list.Select(ToDictionary).ToList());
				}

				break;

			case NbtCompound compound:
				dict.Add("value", compound.Select(kvp => {
					var itemDict = ToDictionary(kvp.Value);
					itemDict["name"] = kvp.Key;
					return itemDict;
				}).ToList());
				break;

			default:
				dict.Add("value", GetValueAsObject(tag));
				break;
		}

		return dict;
	}

	/// <summary>
	/// Get the value of an NBT tag as an object.
	/// </summary>
	private static object? GetValueAsObject(NbtTag tag) {
		return tag switch {
			NbtByte b => b.Value,
			NbtShort s => s.Value,
			NbtInt i => i.Value,
			NbtLong l => l.Value,
			NbtFloat f => f.Value,
			NbtDouble d => d.Value,
			NbtString str => str.Value,
			NbtByteArray ba => ba.Values,
			NbtIntArray ia => ia.Values,
			NbtLongArray la => la.Values,
			NbtList list => list.Select(GetValueAsObject).ToList(),
			NbtCompound compound => compound.Select(kvp => kvp)
				.ToDictionary(kvp => kvp.Key, kvp => GetValueAsObject(kvp.Value)),
			_ => null
		};
	}

	/// <summary>
	/// Get the value of an NBT tag as a string.
	/// </summary>
	private static string? GetValueAsString(NbtTag tag) {
		return tag switch {
			NbtByte b => b.Value.ToString(),
			NbtShort s => s.Value.ToString(),
			NbtInt i => i.Value.ToString(),
			NbtLong l => l.Value.ToString(),
			NbtFloat f => f.Value.ToString(CultureInfo.InvariantCulture),
			NbtDouble d => d.Value.ToString(CultureInfo.InvariantCulture),
			NbtString str => str.Value,
			_ => null
		};
	}

	/// <summary>
	/// Check if an NBT tag is a simple value type (not compound or list).
	/// </summary>
	private static bool IsSimpleType(NbtTag tag) {
		return tag.Type != NbtTagType.Compound && tag.Type != NbtTagType.List;
	}
	
	/// <summary>
	/// Check if an NBT tag is an int (or byte/short) type
	/// </summary>
	private static bool IsIntType(NbtTag tag) {
		return tag.Type is NbtTagType.Byte or NbtTagType.Short or NbtTagType.Int;
	}
	
	/// <summary>
	/// Get the value of an NBT tag as a string.
	/// </summary>
	private static int? GetValueAsInt(NbtTag tag) {
		return tag switch {
			NbtByte b => b.Value,
			NbtShort s => s.Value,
			NbtInt i => i.Value,
			NbtLong l => (int?) l.Value,
			NbtFloat f => (int?) f.Value,
			NbtDouble d => (int?) d.Value,
			NbtString str => int.TryParse(str.Value, out var i) ? i : null,
			_ => null
		};
	}
}