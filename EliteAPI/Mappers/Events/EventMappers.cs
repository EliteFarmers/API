using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Mappers.Events; 

public class EventMappers : Profile {
    public EventMappers() {
        CreateMap<Event, EventDetailsDto>()
            .ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
            .ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()));
    }
}