using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities;

namespace EliteAPI.Mappers.ProfilesData;

public class AccountMapper : Profile
{
    public AccountMapper()
    {
        CreateMap<AccountEntities, AuthorizedAccountDto>()
            .ForMember(a => a.Inventory, opt => opt.MapFrom(a => a.Inventory))
            .ForMember(a => a.Redemptions, opt => opt.MapFrom(a => a.Redemptions))
            .ForMember(a => a.Settings, opt => opt.MapFrom(a => a.Settings))
            .ForMember(a => a.MinecraftAccounts, opt => opt.MapFrom(a => a.MinecraftAccounts));
    }
}

public class MinecraftAccountMapper : Profile
{
    public MinecraftAccountMapper() {
        CreateMap<MinecraftAccount, MinecraftAccountDto>()
            .ForMember(a => a.PrimaryAccount, opt => opt.MapFrom(a => a.Selected))
            .ForMember(a => a.Properties, opt => opt.MapFrom(a => a.Properties));

        CreateMap<MinecraftAccount, MinecraftAccountDetailsDto>()
            .ForMember(a => a.PrimaryAccount, opt => opt.MapFrom(a => a.Selected));
    }
}

public class MinecraftAccountPropertyMapper : Profile
{
    public MinecraftAccountPropertyMapper()
    {
        CreateMap<MinecraftAccountProperty, MinecraftAccountPropertyDto>();
    }
}

public class EliteMapper : Profile
{
    public EliteMapper()
    {
        CreateMap<EliteSettings, EliteSettingsDto>();

        CreateMap<EliteInventory, EliteInventoryDto>()
            .ForMember(x => x.SpentMedals, opt => opt.MapFrom(x => x.SpentMedals))
            .ForMember(x => x.TotalEarnedMedals, opt => opt.MapFrom(x => x.TotalEarnedMedals));
    }
}