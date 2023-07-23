using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Mappers.Discord; 

public class GuildMapper : Profile {
    public GuildMapper() {
        CreateMap<Guild, GuildDto>()
            .ForMember(g => g.AdminRole, opt => opt.MapFrom(g => g.AdminRole.ToString()));
    }
}