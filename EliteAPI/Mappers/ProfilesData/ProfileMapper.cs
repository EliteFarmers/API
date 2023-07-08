using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class ProfileMapper : Profile
{
    public ProfileMapper()
    {
        CreateMap<Models.Entities.Hypixel.Profile, ProfileDto>()
            .ForMember(x => x.Members, opt => opt.MapFrom(x => x.Members));
    }
}

public class ProfileMemberMapper : Profile
{
    public ProfileMemberMapper()
    {
        CreateMap<ProfileMember, ProfileMemberDto>()
            .ForMember(x => x.Collections, opt => opt.MapFrom(x => x.Collections))
            .ForMember(x => x.CollectionTiers, opt => opt.MapFrom(x => x.CollectionTiers))
            .ForMember(x => x.CraftedMinions, opt => opt.MapFrom(x => x.Profile.CraftedMinions))
            .ForMember(x => x.Jacob, opt => opt.MapFrom(x => x.JacobData))
            .ForMember(x => x.Pets, opt => opt.MapFrom(x => x.Pets))
            .ForMember(x => x.Skills, opt => opt.MapFrom(x => x.Skills))
            .ForMember(x => x.BankBalance, opt => opt.MapFrom(x => x.Profile.BankBalance))
            .ForMember(x => x.FarmingWeight, opt => opt.MapFrom(x => x.FarmingWeight));

        CreateMap<ProfileMember, MemberDetailsDto>()
            .ForMember(x => x.Uuid, opt => opt.MapFrom(x => x.PlayerUuid))
            .ForMember(x => x.Username, opt => opt.MapFrom(x => x.MinecraftAccount.Name))
            .ForMember(x => x.Active, opt => opt.MapFrom(x => x.MinecraftAccount.Profiles.Exists(p => p.ProfileId.Equals(x.ProfileId) && !p.WasRemoved)));
    }
}