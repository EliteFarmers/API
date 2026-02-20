using System.Text.Json;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Tests.DtoTests;

public class ProfileMemberDataDtoTests
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	[Fact]
	public void Serialize_ShouldOmitGardenChips_WhenNull() {
		var dto = new ProfileMemberDataDto {
			Attributes = new Dictionary<string, int> {
				{ "THUNDER_SHARDS", 32 }
			},
			Garden = new ProfileMemberGardenDataDto {
				Copper = 123,
				DnaMilestone = 7,
				Chips = null
			}
		};

		var json = JsonSerializer.Serialize(dto, JsonOptions);

		json.ShouldContain("\"garden\"");
		json.ShouldNotContain("\"chips\"");
	}

	[Fact]
	public void Serialize_ShouldIncludeGardenChips_WhenPresent() {
		var dto = new ProfileMemberDataDto {
			Garden = new ProfileMemberGardenDataDto {
				Chips = new ProfileMemberGardenChipsDataDto {
					Cropshot = 1
				}
			}
		};

		var json = JsonSerializer.Serialize(dto, JsonOptions);

		json.ShouldContain("\"chips\"");
		json.ShouldContain("\"cropshot\":1");
	}

	[Fact]
	public void Serialize_ShouldOmitNullChipFields_WhenPartialDataExists() {
		var dto = new ProfileMemberDataDto {
			Garden = new ProfileMemberGardenDataDto {
				Chips = new ProfileMemberGardenChipsDataDto {
					Cropshot = null,
					Rarefinder = 4
				}
			}
		};

		var json = JsonSerializer.Serialize(dto, JsonOptions);

		json.ShouldContain("\"chips\"");
		json.ShouldContain("\"rarefinder\":4");
		json.ShouldNotContain("\"cropshot\"");
	}
}
