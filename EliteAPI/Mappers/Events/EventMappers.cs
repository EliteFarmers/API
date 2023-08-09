using System.Globalization;
using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Mappers.Events; 

public class EventMappers : Profile {
    public EventMappers() {
        CreateMap<Event, EventDetailsDto>()
            .ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
            .ForMember(e => e.StartTime, opt => opt.MapFrom(e => e.StartTime.ToUnixTimeSeconds().ToString()))
            .ForMember(e => e.EndTime, opt => opt.MapFrom(e => e.EndTime.ToUnixTimeSeconds().ToString()))
            .ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()));
    }
}

public class EventMemberMappers : Profile {
    public EventMemberMappers() {
        CreateMap<EventMember, EventMemberDto>()
            .ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
            .ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
            .ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
            .ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
            .ForMember(e => e.AmountGained, opt => opt.MapFrom(e => e.AmountGained.ToString(CultureInfo.InvariantCulture)));
        
        CreateMap<EventMember, EventMemberDetailsDto>()
            .ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
            .ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
            .ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
            .ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
            .ForMember(e => e.AmountGained, opt => opt.MapFrom(e => e.AmountGained.ToString(CultureInfo.InvariantCulture)));
        
        CreateMap<EventMember, EventMemberBannedDto>()
            .ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
            .ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
            .ForMember(e => e.AmountGained, opt => opt.MapFrom(e => e.AmountGained.ToString(CultureInfo.InvariantCulture)));
    }
}