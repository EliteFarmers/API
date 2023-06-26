using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Mappers.FarmingWeight;

public class FarmingWeightMapper : Profile
{
    public FarmingWeightMapper()
    {
        CreateMap<Models.Entities.FarmingWeight, FarmingWeightDto>()
            .ForMember(x => x.TotalWeight, opt => opt.MapFrom(x => x.TotalWeight))
            .ForMember(x => x.CropWeight, opt => opt.MapFrom(x => x.CropWeight))
            .ForMember(x => x.BonusWeight, opt => opt.MapFrom(x => x.BonusWeight));
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