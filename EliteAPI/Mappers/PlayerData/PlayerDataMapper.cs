using System.Globalization;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.PlayerData;

public class PlayerDataMapper : Profile
{
	public PlayerDataMapper() {
		CreateMap<Models.Entities.Hypixel.PlayerData, Models.Entities.Hypixel.PlayerData>()
			.ForMember(x => x.Id, opt => opt.Ignore());

		CreateMap<PlayerDataResponse, Models.Entities.Hypixel.PlayerData>()
			.ForMember(p => p.Karma, opt => opt.MapFrom(x => Convert.ToInt64(x.Karma, CultureInfo.InvariantCulture)))
			.ForMember(p => p.SocialMedia, opt => opt.MapFrom(x => x.SocialMedia != null && x.SocialMedia.Links != null
				? new SocialMediaLinks {
					Hypixel = x.SocialMedia.Links.Hypixel,
					Youtube = x.SocialMedia.Links.Youtube,
					Discord = x.SocialMedia.Links.Discord
				}
				: new SocialMediaLinks()
			));

		CreateMap<Models.Entities.Hypixel.PlayerData, PlayerDataDto>()
			.ForMember(p => p.GuildMember, opt => opt.MapFrom(x => x.MinecraftAccount != null && x.MinecraftAccount.GuildMembers.Count > 0 ? x.MinecraftAccount.GuildMembers.First().ToDetailsDto() : null))
			.ForMember(p => p.SocialMedia, opt => opt.MapFrom(x => x.SocialMedia));
	}
}

public class SocialMediaLinksMapper : Profile
{
	public SocialMediaLinksMapper() {
		CreateMap<SocialMediaLinks, SocialMediaLinksDto>();
	}
}