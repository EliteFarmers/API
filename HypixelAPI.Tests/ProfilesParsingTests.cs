using System.Reflection;
using System.Text.Json;
using EliteFarmers.HypixelAPI.DTOs;
using Xunit;

namespace HypixelAPI.Tests;

public class ProfilesParsingTests
{
	public static IEnumerable<object[]> ProfileJsonFiles()
	{
		var baseDir = AppContext.BaseDirectory;
		var profilesDir = Path.Combine(baseDir, "test-data", "profiles");
		if (!Directory.Exists(profilesDir))
			yield break;

		foreach (var file in Directory.EnumerateFiles(profilesDir, "*.json", SearchOption.TopDirectoryOnly))
		{
			yield return new object[] { file };
		}
	}

	[Theory]
	[MemberData(nameof(ProfileJsonFiles))]
	public void Deserialize_All_Profile_Samples_Without_Error(string filePath)
	{
		var json = File.ReadAllText(filePath);

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true
		};

		var result = JsonSerializer.Deserialize<ProfilesResponse>(json, options);

		Assert.NotNull(result);
		Assert.True(result!.Success);
		Assert.NotNull(result.Profiles);
		Assert.NotEmpty(result.Profiles!);

		foreach (var profile in result.Profiles!)
		{
			Assert.NotNull(profile.CuteName);
			Assert.False(string.IsNullOrWhiteSpace(profile.ProfileId));
			Assert.NotNull(profile.Members);
			Assert.NotEmpty(profile.Members);

			// Light-touch checks on a few mapped areas for resilience
			foreach (var member in profile.Members.Values)
			{
				// Collections, currencies, and inventories should deserialize without throwing
				_ = member.Collection; // may be null for some samples
				_ = member.Currencies;
				_ = member.Inventories;
				_ = member.Events;
				_ = member.Leveling;
				_ = member.PetsData;
				_ = member.Dungeons;
				_ = member.Rift;

				// If Easter rabbits mapping exists, ensure values are non-negative
				var rabbits = member.Events?.Easter?.Rabbits;
				if (rabbits is not null)
				{
					Assert.All(rabbits, kv => Assert.True(kv.Value >= 0));
				}

				var rift = member.Rift;
				if (rift is not null)
				{
					Assert.NotNull(rift.AdditionalData);
					Assert.All(rift.AdditionalData.Keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));

					if (rift.Access is not null)
					{
						_ = rift.Access.LastFree;
						_ = rift.Access.ConsumedPrism;
						_ = rift.Access.ChargeTrackTimestamp;
					}

					if (rift.Inventory is not null)
					{
						_ = rift.Inventory.InventoryContents;
						_ = rift.Inventory.Armor;
						_ = rift.Inventory.EquipmentContents;
						_ = rift.Inventory.EnderChestContents;
						if (rift.Inventory.EnderChestPageIcons is not null)
						{
							Assert.All(rift.Inventory.EnderChestPageIcons, _ => { });
						}
					}
				}

				// If dungeons present, experiences should be >= 0
				var d = member.Dungeons;
				if (d?.DungeonTypes is not null)
				{
					Assert.True(d.DungeonTypes.Catacombs.Experience >= 0);
					Assert.True(d.DungeonTypes.MasterCatacombs.Experience >= 0);
				}
				if (d?.PlayerClasses is not null)
				{
					Assert.True(d.PlayerClasses.Archer.Experience >= 0);
					Assert.True(d.PlayerClasses.Berserk.Experience >= 0);
					Assert.True(d.PlayerClasses.Healer.Experience >= 0);
					Assert.True(d.PlayerClasses.Mage.Experience >= 0);
					Assert.True(d.PlayerClasses.Tank.Experience >= 0);
				}
			}
		}
	}
}
