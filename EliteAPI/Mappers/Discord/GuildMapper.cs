using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services;

namespace EliteAPI.Mappers.Discord;

public class GuildMapper : Profile
{
	public GuildMapper() {
		CreateMap<Guild, PrivateGuildDto>()
			.ForMember(g => g.Public, opt => opt.MapFrom(g => g.IsPublic))
			.ForMember(g => g.AdminRole, opt => opt.MapFrom(g => g.AdminRole.ToString()))
			.ForMember(g => g.BotPermissions, opt => opt.MapFrom(g => g.BotPermissions.ToString()))
			.ForMember(g => g.DiscordFeatures, opt => opt.MapFrom(g => g.DiscordFeatures))
			.ForMember(g => g.Icon, opt => opt.MapFrom(g => g.Icon))
			.ForMember(g => g.Banner, opt => opt.MapFrom(g => g.Banner))
			.ForMember(g => g.Id, opt => opt.MapFrom(g => g.Id.ToString()))
			.ForMember(g => g.Roles, opt => opt.MapFrom(g => g.Roles.OrderByDescending(r => r.Position).ToList()))
			.ForMember(g => g.Channels, opt => opt.MapFrom(g => g.Channels.OrderBy(r => r.Position).ToList()));

		CreateMap<Guild, PublicGuildDto>()
			.ForMember(g => g.Icon, opt => opt.MapFrom(g => g.Icon))
			.ForMember(g => g.Banner, opt => opt.MapFrom(g => g.Banner))
			.ForMember(g => g.Features, opt => opt.MapFrom(g => g.Features))
			.ForMember(g => g.Id, opt => opt.MapFrom(g => g.Id.ToString()));

		CreateMap<Guild, GuildDetailsDto>()
			.ForMember(g => g.Id, opt => opt.MapFrom(g => g.Id.ToString()))
			.ForMember(g => g.MemberCount, opt => opt.MapFrom(g => g.MemberCount))
			.ForMember(g => g.Icon, opt => opt.MapFrom(g => g.Icon))
			.ForMember(g => g.Banner, opt => opt.MapFrom(g => g.Banner))
			.ForMember(g => g.InviteCode, opt => opt.MapFrom(g => g.InviteCode))
			.ForMember(g => g.Name, opt => opt.MapFrom(g => g.Name));

		CreateMap<DiscordGuild, GuildMemberDto>()
			.ForMember(g => g.Permissions, opt => opt.MapFrom(g => g.Permissions.ToString()));

		CreateMap<GuildChannel, GuildChannelDto>()
			.ForMember(g => g.Id, opt => opt.MapFrom(g => g.Id.ToString()));
		CreateMap<GuildRole, GuildRoleDto>()
			.ForMember(g => g.Id, opt => opt.MapFrom(g => g.Id.ToString()));

		CreateMap<GuildMember, GuildMemberDto>()
			.ForMember(g => g.Id, opt => opt.MapFrom(g => g.GuildId.ToString()))
			.ForMember(g => g.Name, opt => opt.MapFrom(g => g.Guild.Name))
			.ForMember(g => g.Icon, opt => opt.MapFrom(g => g.Guild.Icon))
			.ForMember(g => g.HasBot, opt => opt.MapFrom(g => g.Guild.HasBot))
			.ForMember(g => g.Permissions, opt => opt.MapFrom(g => g.Permissions.ToString()))
			.ForMember(g => g.Roles, opt => opt.MapFrom(g => g.Roles.Select(r => r.ToString()).ToList()))
			.ForMember(g => g.Admin, opt => opt.MapFrom(g => g.HasGuildAdminPermissions(GuildPermission.Role)));
	}
}

public class GuildFeaturesMapper : Profile
{
	public GuildFeaturesMapper() {
		CreateMap<GuildFeatures, PublicGuildFeaturesDto>()
			.ForMember(g => g.JacobLeaderboard, opt => opt.MapFrom(g => g.JacobLeaderboard));

		CreateMap<GuildJacobLeaderboardFeature, PublicJacobLeaderboardFeatureDto>()
			.ForMember(g => g.Leaderboards, opt => opt.MapFrom(g => g.Leaderboards));

		CreateMap<GuildJacobLeaderboard, PublicJacobLeaderboardDto>();

		CreateMap<ContestPingsFeature, ContestPingsFeatureDto>();
	}
}