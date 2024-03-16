using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;

namespace EliteAPI.Mappers.Accounts;

public class AccountMapper : Profile
{
    public AccountMapper() {
        CreateMap<EliteAccount, AuthorizedAccountDto>()
            .ForMember(a => a.Inventory, opt => opt.MapFrom(a => a.Inventory))
            .ForMember(a => a.Redemptions, opt => opt.MapFrom(a => a.Redemptions))
            .ForMember(a => a.Settings, opt => opt.MapFrom(a => a.Settings))
            .ForMember(a => a.MinecraftAccounts, opt => opt.MapFrom(a => a.MinecraftAccounts))
            .ForMember(a => a.Permissions, opt => opt.MapFrom(a => (int) a.Permissions))
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()));

        CreateMap<EliteAccount, AccountWithPermsDto>()
            .ForMember(a => a.Permissions, opt => opt.MapFrom(a => (int) a.Permissions))
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()));
    }
}

public class MinecraftAccountMapper : Profile
{
    public MinecraftAccountMapper() {
        CreateMap<MinecraftAccount, MinecraftAccountDto>()
            .ForMember(a => a.PrimaryAccount, opt => opt.MapFrom(a => a.Selected))
            .ForMember(a => a.Properties, opt => opt.MapFrom(a => a.Properties))
            .ForMember(a => a.Badges, opt => opt.MapFrom(a => a.Badges))
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()));

        CreateMap<MinecraftAccount, MinecraftAccountDetailsDto>()
            .ForMember(a => a.PrimaryAccount, opt => opt.MapFrom(a => a.Selected))
            .ForMember(a => a.Badges, opt => opt.MapFrom(a => a.Badges))
            .ForMember(a => a.Properties, opt => opt.MapFrom(a => a.Properties));
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