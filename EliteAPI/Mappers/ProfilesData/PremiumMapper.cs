using EliteAPI.Models;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Mappers.ProfilesData;
using Profile = AutoMapper.Profile;

public class PremiumMapper : Profile
{
    public PremiumMapper()
    {
        CreateMap<Premium, PremiumDto>()
            .ForMember(x => x.Purchases, x => x.MapFrom(y => y.Purchases));
    }
}

public class PurchaseMapper : Profile
{
    public PurchaseMapper()
    {
        CreateMap<Purchase, PurchaseDto>();
    }
}