﻿using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Hypixel;
using Profile = AutoMapper.Profile;
using Skill = EliteAPI.Models.Hypixel.Skill;

namespace EliteAPI.Mappers.ProfilesData;

public class ProfileMapper : Profile
{
    public ProfileMapper()
    {
        CreateMap<EliteAPI.Models.Hypixel.Profile, ProfileDto>()
            .ForMember(x => x.Members, opt => opt.MapFrom(x => x.Members))
            .ForMember(x => x.Banking, opt => opt.MapFrom(x => x.Banking))
            .ForMember(x => x.CraftedMinions, opt => opt.MapFrom(x => x.CraftedMinions));
    }
}

public class ProfileMemberMapper : Profile
{
    public ProfileMemberMapper()
    {
        CreateMap<ProfileMember, ProfileMemberDto>()
            .ForMember(x => x.Collections, opt => opt.MapFrom(x => x.Collections))
            .ForMember(x => x.JacobData, opt => opt.MapFrom(x => x.JacobData))
            .ForMember(x => x.Pets, opt => opt.MapFrom(x => x.Pets))
            .ForMember(x => x.Skills, opt => opt.MapFrom(x => x.Skills));
    }
}