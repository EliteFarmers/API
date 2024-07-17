using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Mappers.Accounts;

public class AccountMapper : Profile
{
    public AccountMapper() {
        CreateMap<EliteAccount, AuthorizedAccountDto>()
            .ForMember(a => a.Entitlements, opt => opt.MapFrom(a => a.Entitlements))
            .ForMember(a => a.Settings, opt => opt.MapFrom(a => a.UserSettings))
            .ForMember(a => a.MinecraftAccounts, opt => opt.MapFrom(a => a.MinecraftAccounts))
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
        CreateMap<UserSettings, UserSettingsDto>();

        CreateMap<Entitlement, UserEntitlementDto>()
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()))
            .ForMember(a => a.Product, opt => opt.MapFrom(a => a.Product))
            .ForMember(a => a.ProductId, opt => opt.MapFrom(a => a.ProductId.ToString()));
        
        CreateMap<UserEntitlement, UserEntitlementDto>()
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()))
            .ForMember(a => a.Product, opt => opt.MapFrom(a => a.Product))
            .ForMember(a => a.ProductId, opt => opt.MapFrom(a => a.ProductId.ToString()));

        CreateMap<Product, ProductDto>()
            .ForMember(p => p.Id, opt => opt.MapFrom(p => p.Id.ToString()))
            .ForMember(p => p.Features, opt => opt.MapFrom(p => p.Features));
        
        CreateMap<UnlockedProductFeatures, UnlockedProductFeaturesDto>();
        CreateMap<ConfiguredProductFeatures, ConfiguredProductFeaturesDto>();
    }
}