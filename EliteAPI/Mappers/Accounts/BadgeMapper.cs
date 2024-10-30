using AutoMapper;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Mappers.Accounts;

public class BadgeMapper : Profile {
    public BadgeMapper() {
        CreateMap<Badge, BadgeDto>()
            .ForMember(b => b.Id, opt => opt.MapFrom(b => b.Id))
            .ForMember(b => b.Image, opt => opt.MapFrom(b => b.Image))
            .ForMember(b => b.Name, opt => opt.MapFrom(b => b.Name))
            .ForMember(b => b.Description, opt => opt.MapFrom(b => b.Description))
            .ForMember(b => b.Requirements, opt => opt.MapFrom(b => b.Requirements))
            .ReverseMap();
        
        CreateMap<Badge, EditBadgeDto>();
        CreateMap<EditBadgeDto, Badge>();
        
        CreateMap<UserBadge, UserBadgeDto>()
            .ForMember(b => b.Id, opt => opt.MapFrom(b => b.BadgeId))
            .ForMember(b => b.Image, opt => opt.MapFrom(b => b.Badge.Image))
            .ForMember(b => b.Name, opt => opt.MapFrom(b => b.Badge.Name))
            .ForMember(b => b.Description, opt => opt.MapFrom(b => b.Badge.Description))
            .ForMember(b => b.Requirements, opt => opt.MapFrom(b => b.Badge.Requirements))
            .ForMember(b => b.Timestamp, opt => opt.MapFrom(b => b.Timestamp.ToUnixTimeSeconds().ToString()));
    }
}