using System.Text.Json;
using EliteAPI.Features.Leaderboards;
using EliteAPI.Features.Profiles;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class ProfileMapper : Profile
{
    public ProfileMapper()
    {
        CreateMap<Models.Entities.Hypixel.Profile, ProfileDetailsDto>()
            .ForMember(x => x.Deleted, opt => opt.MapFrom(x => x.IsDeleted))
            .ForMember(x => x.Members, opt => opt.MapFrom(x => x.Members));
    }
}

public class ProfileMemberMapper : Profile
{
    private static readonly JsonSerializerOptions CollectionOptions = new JsonSerializerOptions();
    
    public ProfileMemberMapper() {
        CreateMap<ProfileMember, ProfileMemberDto>()
            .ForMember(x => x.ProfileName, opt => opt.MapFrom(x => x.ProfileName ?? x.Profile.ProfileName))
            .ForMember(x => x.Collections, opt =>
                opt.MapFrom(x => x.Collections.Deserialize<Dictionary<string, long>>(CollectionOptions)))
            .ForMember(x => x.CollectionTiers, opt => opt.MapFrom(x => x.CollectionTiers))
            .ForMember(x => x.CraftedMinions, opt => opt.MapFrom(x => x.Profile.CraftedMinions))
            .ForMember(x => x.Jacob, opt => opt.MapFrom(x => x.JacobData))
            .ForMember(x => x.Pets, opt => opt.MapFrom(x => x.Pets))
            .ForMember(x => x.Skills, opt => opt.MapFrom(x => x.Skills))
            .ForMember(x => x.BankBalance, opt => opt.MapFrom(x => x.Profile.BankBalance))
            .ForMember(x => x.FarmingWeight, opt => opt.MapFrom(x => x.Farming))
            .ForMember(x => x.Garden, opt => opt.MapFrom(x => x.Profile.Garden))
            .ForMember(x => x.Unparsed, opt => opt.MapFrom(x => x.Unparsed))
            .ForMember(x => x.ChocolateFactory, opt => opt.MapFrom(x => x.ChocolateFactory))
            .ForMember(x => x.Api, opt => opt.MapFrom(x => x.Api))
            .ForMember(x => x.Meta, opt => opt.MapFrom(x => x.GetCosmeticsDto()))
            .ForMember(x => x.Events, opt => opt.MapFrom(x => x.EventEntries));

        CreateMap<ProfileMember, MemberDetailsDto>()
            .ForMember(x => x.Uuid, opt => opt.MapFrom(x => x.PlayerUuid))
            .ForMember(x => x.ProfileName, opt => opt.MapFrom(x => x.ProfileName ?? x.Profile.ProfileName))
            .ForMember(x => x.Username, opt => opt.MapFrom(x => x.MinecraftAccount.Name))
            .ForMember(x => x.FarmingWeight, opt => opt.MapFrom(x => x.Farming.TotalWeight))
            .ForMember(x => x.Meta, opt => opt.MapFrom(x => x.GetCosmeticsDto()))
            .ForMember(x => x.Active, opt => opt.MapFrom(x => !x.WasRemoved));
    }
}

public class ApiDataMapper : Profile {
    public ApiDataMapper() {
        CreateMap<ApiAccess, ApiAccessDto>();

        CreateMap<UnparsedApiData, UnparsedApiDataDto>()
            .ForMember(x => x.AccessoryBagSettings, opt => opt.MapFrom(x => x.AccessoryBagSettings))
            .ForMember(x => x.Bestiary, opt => opt.MapFrom(x => x.Bestiary));
    }
}

public class InventoriesMapper : Profile {
    public InventoriesMapper() {
        CreateMap<Models.Entities.Hypixel.Inventories, InventoriesDto>()
            .ForMember(x => x.Talismans, opt => opt.MapFrom(x => x.TalismanBag))
            .ForMember(x => x.Vault, opt => opt.MapFrom(x => x.PersonalVault));
    }
}