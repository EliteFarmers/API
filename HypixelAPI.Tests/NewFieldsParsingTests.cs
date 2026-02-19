using System.Text.Json;
using EliteFarmers.HypixelAPI.DTOs;

namespace HypixelAPI.Tests;

public class NewFieldsParsingTests
{
	[Fact]
	public void Deserialize_NewProfileMemberFields() {
		const string json = """
		                    {
		                      "success": true,
		                      "profiles": [
		                        {
		                          "cute_name": "Test",
		                          "profile_id": "profile-1",
		                          "members": {
		                            "player-1": {
		                              "player_data": {
		                                "experience": {
		                                  "SKILL_HUNTING": 42
		                                },
		                                "garden_chips": {
		                                  "cropshot": 3,
		                                  "rarefinder": 7
		                                }
		                              },
		                              "attributes": {
		                                "stacks": {
		                                  "THUNDER_SHARDS": 32
		                                }
		                              },
		                              "shards": {
		                                "owned": [
		                                  {
		                                    "type": "THUNDER",
		                                    "amount_owned": 12,
		                                    "captured": 123456
		                                  }
		                                ]
		                              },
		                              "garden_player_data": {
		                                "analyzed_greenhouse_crops": [ "alpha" ],
		                                "discovered_greenhouse_crops": [ "beta" ]
		                              }
		                            }
		                          }
		                        }
		                      ]
		                    }
		                    """;

		var options = new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true
		};

		var parsed = JsonSerializer.Deserialize<ProfilesResponse>(json, options);
		Assert.NotNull(parsed);
		Assert.True(parsed!.Success);
		Assert.NotNull(parsed.Profiles);
		Assert.Single(parsed.Profiles!);

		var member = parsed.Profiles![0].Members["player-1"];
		Assert.Equal(42, member.PlayerData?.Experience?.SkillHunting);
		Assert.Equal(32, member.Attributes.Stacks["THUNDER_SHARDS"]);
		Assert.Single(member.Shards.Owned);
		Assert.Equal("THUNDER", member.Shards.Owned[0].Type);
		Assert.Equal(12, member.Shards.Owned[0].Owned);
		Assert.Equal(123456, member.Shards.Owned[0].CapturedAt);
		Assert.Equal(3, member.PlayerData?.GardenChips.Cropshot);
		Assert.Equal(7, member.PlayerData?.GardenChips.Rarefinder);
		Assert.Equal("alpha", member.Garden?.AnalyzedGreenhouseCrops[0]);
		Assert.Equal("beta", member.Garden?.DiscoveredGreenhouseCrops[0]);
	}

	[Fact]
	public void Deserialize_NewGardenFields() {
		const string json = """
		                    {
		                      "success": true,
		                      "garden": {
		                        "uuid": "profile-1",
		                        "garden_experience": 1,
		                        "unlocked_plots_ids": [],
		                        "commission_data": {
		                          "total_completed": 0,
		                          "unique_npcs_served": 0
		                        },
		                        "resources_collected": {},
		                        "crop_upgrade_levels": {},
		                        "composter_data": {
		                          "organic_matter": 0,
		                          "fuel_units": 0,
		                          "compost_units": 0,
		                          "compost_items": 0,
		                          "conversion_ticks": 0,
		                          "last_save": 0,
		                          "upgrades": {}
		                        },
		                        "active_commissions": {},
		                        "last_growth_stage_time": 999,
		                        "greenhouse_slots": [
		                          { "x": 1, "z": 2 },
		                          { "x": 8, "z": 7 }
		                        ],
		                        "garden_upgrades": {
		                          "YIELD": 6,
		                          "GROWTH_SPEED": 4,
		                          "PLOT_LIMIT": 2
		                        }
		                      }
		                    }
		                    """;

		var options = new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true
		};

		var parsed = JsonSerializer.Deserialize<GardenResponse>(json, options);
		Assert.NotNull(parsed);
		Assert.True(parsed!.Success);
		Assert.NotNull(parsed.Garden);
		Assert.Equal(999, parsed.Garden!.LastGrowthStageTime);
		Assert.Equal(2, parsed.Garden.GreenhouseSlots.Count);
		Assert.Equal(1, parsed.Garden.GreenhouseSlots[0].X);
		Assert.Equal(2, parsed.Garden.GreenhouseSlots[0].Z);
		Assert.Equal(6, parsed.Garden.GardenUpgrades.GreenhouseYield);
		Assert.Equal(2, parsed.Garden.GardenUpgrades.GreenhousePlotLimit);
		Assert.Equal(4, parsed.Garden.GardenUpgrades.GreenhouseGrowthSpeed);
	}
}
