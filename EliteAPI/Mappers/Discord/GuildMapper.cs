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

        CreateMap<Guild, PublicGuildDto>()
            .ForMember(g => g.Features, opt => opt.MapFrom(g => g.Features));

        CreateMap<DiscordGuild, UserGuildDto>()
            .ForMember(g => g.Permissions, opt => opt.MapFrom(g => g.Permissions.ToString()));
    }
}

public class GuildFeaturesMapper : Profile {
    public GuildFeaturesMapper() {
        CreateMap<GuildFeatures, PublicGuildFeaturesDto>()
            .ForMember(g => g.JacobLeaderboard, opt => opt.MapFrom(g => g.JacobLeaderboard));

        CreateMap<GuildJacobLeaderboardFeature, PublicJacobLeaderboardFeatureDto>()
            .ForMember(g => g.Leaderboards, opt => opt.MapFrom(g => g.Leaderboards));
            
        CreateMap<GuildJacobLeaderboard, PublicJacobLeaderboardDto>();

        CreateMap<ContestPingsFeature, ContestPingsFeatureDto>();
    }
}