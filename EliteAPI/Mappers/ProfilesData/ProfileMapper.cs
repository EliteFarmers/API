using System.Text.Json;
using EliteAPI.Features.Profiles;
using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class ProfileMapper : Profile
{
	public ProfileMapper() {
		CreateMap<Models.Entities.Hypixel.Profile, ProfileDetailsDto>()
			.ForMember(x => x.Deleted, opt => opt.MapFrom(x => x.IsDeleted))
			.ForMember(x => x.Members, opt => opt.MapFrom(x => x.Members));
	}
}

public class ProfileMemberMapper : Profile
{
	private static readonly JsonSerializerOptions CollectionOptions = new();

	public ProfileMemberMapper() {
		CreateMap<ProfileMember, ProfileMemberDto>()
			.ForMember(x => x.ProfileName, opt => opt.MapFrom(x => x.ProfileName ?? x.Profile.ProfileName))
			.ForMember(x => x.Collections, opt => opt.MapFrom(x => x.Collections))
			.ForMember(x => x.CollectionTiers, opt => opt.MapFrom(x => x.CollectionTiers))
			.ForMember(x => x.CraftedMinions, opt => opt.MapFrom(x => x.Profile.CraftedMinions))
			.ForMember(x => x.SocialXp, opt => opt.MapFrom(x => x.Profile.SocialXp))
			.ForMember(x => x.Jacob, opt => opt.MapFrom(x => x.JacobData))
			.ForMember(x => x.Pets, opt => opt.MapFrom(x => x.Pets))
			.ForMember(x => x.Skills, opt => opt.MapFrom(x => x.Skills))
			.ForMember(x => x.BankBalance, opt => opt.MapFrom(x => x.Profile.BankBalance))
			.ForMember(x => x.FarmingWeight, opt => opt.MapFrom(x => x.Farming))
			.ForMember(x => x.Garden, opt => opt.MapFrom(x => x.Profile.Garden))
			.ForMember(x => x.Unparsed, opt => opt.MapFrom(x => x.Unparsed))
			.ForMember(x => x.MemberData, opt => opt.Ignore())
			.ForMember(x => x.ChocolateFactory, opt => opt.MapFrom(x => x.ChocolateFactory))
			.ForMember(x => x.Api, opt => opt.MapFrom(x => x.Api))
			.ForMember(x => x.Meta, opt => opt.MapFrom(x => x.GetCosmeticsDto()))
			.ForMember(x => x.Inventories, opt => opt.MapFrom(x => x.Inventories.Select(i => i.ToOverviewDto())))
			.ForMember(x => x.Events, opt => opt.MapFrom(x => x.EventEntries))
			.ForMember(x => x.Networth, opt => opt.MapFrom(x => new ProfileMemberNetworthDto {
				Normal = x.Networth,
				Liquid = x.LiquidNetworth,
				Functional = x.FunctionalNetworth,
				LiquidFunctional = x.LiquidFunctionalNetworth
			}))
			.AfterMap((src, dest) => {
				dest.MemberData = BuildMemberDataDto(src);
			});

		CreateMap<ProfileMember, MemberDetailsDto>()
			.ForMember(x => x.Uuid, opt => opt.MapFrom(x => x.PlayerUuid))
			.ForMember(x => x.ProfileName, opt => opt.MapFrom(x => x.ProfileName ?? x.Profile.ProfileName))
			.ForMember(x => x.Username, opt => opt.MapFrom(x => x.MinecraftAccount.Name))
			.ForMember(x => x.FarmingWeight, opt => opt.MapFrom(x => x.Farming.TotalWeight))
			.ForMember(x => x.Meta, opt => opt.MapFrom(x => x.GetCosmeticsDto()))
			.ForMember(x => x.Active, opt => opt.MapFrom(x => !x.WasRemoved));

		CreateMap<ProfileMemberShardData, ProfileMemberShardDataDto>();
		CreateMap<ProfileMemberGardenChipsData, ProfileMemberGardenChipsDataDto>();
		CreateMap<ProfileMemberMutationData, ProfileMemberMutationDataDto>();
	}

	private static ProfileMemberDataDto BuildMemberDataDto(ProfileMember source) {
		var memberData = source.MemberData ?? new ProfileMemberData();
		var attributeStacks = memberData.AttributeStacks ?? new Dictionary<string, int>();
		var ownedShards = memberData.Shards ?? [];
		var mutations = memberData.Mutations ?? new Dictionary<string, ProfileMemberMutationData>();
		var unparsed = source.Unparsed ?? new UnparsedApiData();

		return new ProfileMemberDataDto {
			Attributes = attributeStacks,
			CapturedShards = ownedShards
				.Select(shard => new ProfileMemberShardDataDto {
					Type = shard.Type,
					Amount = shard.AmountOwned,
					CapturedAt = shard.CapturedAt
				})
				.ToList(),
			Garden = new ProfileMemberGardenDataDto {
				Copper = unparsed.Copper,
				DnaMilestone = unparsed.DnaMilestone,
				Chips = TryBuildGardenChipsDto(memberData.GardenChips),
				Mutations = mutations.ToDictionary(
					kvp => kvp.Key,
					kvp => new ProfileMemberMutationDataDto {
						Analyzed = kvp.Value.Analyzed,
						Discovered = kvp.Value.Discovered
					})
			}
		};
	}

	private static ProfileMemberGardenChipsDataDto? TryBuildGardenChipsDto(ProfileMemberGardenChipsData? chips) {
		if (chips is null) return null;

		var hasAnyValue = chips.Cropshot.HasValue ||
			chips.Sowledge.HasValue ||
			chips.Hypercharge.HasValue ||
			chips.Quickdraw.HasValue ||
			chips.Mechamind.HasValue ||
			chips.Overdrive.HasValue ||
			chips.Synthesis.HasValue ||
			chips.VerminVaporizer.HasValue ||
			chips.Evergreen.HasValue ||
			chips.Rarefinder.HasValue;

		if (!hasAnyValue) return null;

		return new ProfileMemberGardenChipsDataDto {
			Cropshot = chips.Cropshot,
			Sowledge = chips.Sowledge,
			Hypercharge = chips.Hypercharge,
			Quickdraw = chips.Quickdraw,
			Mechamind = chips.Mechamind,
			Overdrive = chips.Overdrive,
			Synthesis = chips.Synthesis,
			VerminVaporizer = chips.VerminVaporizer,
			Evergreen = chips.Evergreen,
			Rarefinder = chips.Rarefinder
		};
	}
}

public class ApiDataMapper : Profile
{
	public ApiDataMapper() {
		CreateMap<ApiAccess, ApiAccessDto>();

		CreateMap<UnparsedApiData, UnparsedApiDataDto>()
			.ForMember(x => x.AccessoryBagSettings, opt => opt.MapFrom(x => x.AccessoryBagSettings))
			.ForMember(x => x.Bestiary, opt => opt.MapFrom(x => x.Bestiary));
	}
}

public class InventoriesMapper : Profile
{
	public InventoriesMapper() {
		CreateMap<Inventories, InventoriesDto>()
			.ForMember(x => x.Talismans, opt => opt.MapFrom(x => x.TalismanBag))
			.ForMember(x => x.Vault, opt => opt.MapFrom(x => x.PersonalVault));
	}
}
