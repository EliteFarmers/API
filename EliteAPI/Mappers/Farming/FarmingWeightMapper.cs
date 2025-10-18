using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Utilities;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.Farming;

public class FarmingWeightMapper : Profile
{
	public FarmingWeightMapper() {
		CreateMap<Models.Entities.Farming.Farming, FarmingWeightDto>()
			.ForMember(x => x.TotalWeight, opt => opt.MapFrom(x => x.TotalWeight))
			.ForMember(x => x.CropWeight, opt => opt.MapFrom(x => x.CropWeight))
			.ForMember(x => x.BonusWeight, opt => opt.MapFrom(x => x.BonusWeight))
			.ForMember(x => x.UncountedCrops, opt => opt.MapFrom(x => x.UncountedCrops
				.ToDictionary(pair => FormatUtils.GetFormattedCropName(pair.Key), pair => pair.Value)
			))
			.ForMember(x => x.Inventory, opt => opt.MapFrom(x => x.Inventory))
			.ForMember(x => x.LastUpdated,
				opt => opt.MapFrom(x => x.ProfileMember == null ? 0 : x.ProfileMember.LastUpdated));

		CreateMap<ProfileMember, FarmingWeightWithProfileDto>()
			.ForMember(x => x.TotalWeight, opt => opt.MapFrom(x => x.Farming.TotalWeight))
			.ForMember(x => x.CropWeight, opt => opt.MapFrom(x => x.Farming.CropWeight))
			.ForMember(x => x.BonusWeight, opt => opt.MapFrom(x => x.Farming.BonusWeight))
			.ForMember(x => x.UncountedCrops, opt => opt.MapFrom(x => x.Farming.UncountedCrops
				.ToDictionary(pair => FormatUtils.GetFormattedCropName(pair.Key), pair => pair.Value)
			))
			.ForMember(x => x.Pests, opt => opt.MapFrom(x => x.Farming.Pests))
			.ForMember(x => x.ProfileId, opt => opt.MapFrom(x => x.ProfileId))
			.ForMember(x => x.ProfileName, opt => opt.MapFrom(x => x.ProfileName ?? x.Profile.ProfileName))
			.ForMember(x => x.LastUpdated, opt => opt.MapFrom(x => x.LastUpdated));
	}
}

public class FarmingInventoryMapper : Profile
{
	public FarmingInventoryMapper() {
		CreateMap<FarmingInventory, FarmingInventoryDto>();
	}
}