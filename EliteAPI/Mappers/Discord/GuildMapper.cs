using AutoMapper;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Mappers.Discord; 

public class GuildMapper : Profile {
    public GuildMapper() {
        CreateMap<Guild, GuildDto>()
            .ForMember(g => g.AdminRole, opt => opt.MapFrom(g => g.AdminRole.ToString()))
            .ForMember(g => g.BotPermissions, opt => opt.MapFrom(g => g.BotPermissions.ToString()))
            .ForMember(g => g.BotPermissionsNew, opt => opt.MapFrom(g => g.BotPermissions.ToString()));

        CreateMap<DiscordGuild, UserGuildDto>()
            .ForMember(g => g.Permissions, opt => opt.MapFrom(g => g.Permissions.ToString()));
    }
}