using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using HypixelAPI.DTOs;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.Farming;

public class GardenMapper : Profile {
	public GardenMapper() {
		CreateMap<ComposterData, ComposterDto>()
			.ForMember(c => c.LastSave, o => o.MapFrom(c => Math.Floor(c.LastSave / 1000f)));

		CreateMap<VisitorData, VisitorDto>();

		CreateMap<CropSettings<long>, CropSettings<string>>()
			.ForMember(c => c.Cactus, o => o.MapFrom(c => c.Cactus.ToString()))
			.ForMember(c => c.Carrot, o => o.MapFrom(c => c.Carrot.ToString()))
			.ForMember(c => c.CocoaBeans, o => o.MapFrom(c => c.CocoaBeans.ToString()))
			.ForMember(c => c.Potato, o => o.MapFrom(c => c.Potato.ToString()))
			.ForMember(c => c.Wheat, o => o.MapFrom(c => c.Wheat.ToString()))
			.ForMember(c => c.Melon, o => o.MapFrom(c => c.Melon.ToString()))
			.ForMember(c => c.Pumpkin, o => o.MapFrom(c => c.Pumpkin.ToString()))
			.ForMember(c => c.Mushroom, o => o.MapFrom(c => c.Mushroom.ToString()))
			.ForMember(c => c.SugarCane, o => o.MapFrom(c => c.SugarCane.ToString()))
			.ForMember(c => c.NetherWart, o => o.MapFrom(c => c.NetherWart.ToString()));

		CreateMap<Garden, GardenDto>()
			.ForMember(g => g.UnlockedPlots, o => o.MapFrom(g => g.UnlockedPlots.SeparatePlots()))
			.ForMember(g => g.Composter, o => o.MapFrom(g => g.Composter))
			.ForMember(g => g.Crops, o => o.MapFrom(g => g.Crops))
			.ForMember(g => g.CropUpgrades, o => o.MapFrom(g => g.Upgrades))
			.ForMember(g => g.LastSave, o => o.MapFrom(g => g.LastUpdated.ToUnixTimeSeconds()));
	}
}