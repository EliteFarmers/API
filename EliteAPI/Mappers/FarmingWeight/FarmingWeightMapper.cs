using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Mappers.FarmingWeight;

public class FarmingWeightMapper : Profile
{
    public FarmingWeightMapper()
    {
        CreateMap<Models.Entities.Farming.Farming, FarmingWeightDto>()
            .ForMember(x => x.TotalWeight, opt => opt.MapFrom(x => x.TotalWeight))
            .ForMember(x => x.CropWeight, opt => opt.MapFrom(x => x.CropWeight))
            .ForMember(x => x.BonusWeight, opt => opt.MapFrom(x => x.BonusWeight));

        CreateMap<Models.Entities.Farming.Farming, FarmingWeightWithProfileDto>()
            .ForMember(x => x.TotalWeight, opt => opt.MapFrom(x => x.TotalWeight))
            .ForMember(x => x.CropWeight, opt => opt.MapFrom(x => x.CropWeight))
            .ForMember(x => x.BonusWeight, opt => opt.MapFrom(x => x.BonusWeight))
            .ForMember(x => x.ProfileId, opt => opt.MapFrom(x => x.ProfileMember != null ? x.ProfileMember.ProfileId : "null"))
            .ForMember(x => x.ProfileName, opt => opt.MapFrom(x => x.ProfileMember != null ? x.ProfileMember.Profile.ProfileName : "null"));
    }
}
/*
public class FarmingInventory : Profile
{
    public FarmingInventory()
    {
        CreateMap<Models.Entities.FarmingInventory, FarmingInventoryDto>();
    }
}
*/