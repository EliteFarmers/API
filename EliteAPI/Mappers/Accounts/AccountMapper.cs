using AutoMapper;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Mappers.Accounts;

public class AccountMapper : Profile
{
    public AccountMapper() {
        CreateMap<EliteAccount, AuthorizedAccountDto>()
            .ForMember(a => a.Entitlements, opt => opt.MapFrom(a => a.ProductAccesses))
            .ForMember(a => a.Settings, opt => opt.MapFrom(a => a.UserSettings))
            .ForMember(a => a.MinecraftAccounts, opt => opt.MapFrom(a => a.MinecraftAccounts))
            .ForMember(a => a.DismissedAnnouncements, opt => opt.MapFrom(a => a.DismissedAnnouncements.Select(d => d.AnnouncementId)))
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()));
    }
}

public class MinecraftAccountMapper : Profile
{
    public MinecraftAccountMapper() {
        CreateMap<MinecraftAccount, MinecraftAccountDto>()
            .ForMember(a => a.FormattedName, opt => opt.MapFrom(a => a.EliteAccount != null ? a.EliteAccount.GetFormattedIgn() : a.Name))
            .ForMember(a => a.PrimaryAccount, opt => opt.MapFrom(a => a.Selected))
            .ForMember(a => a.Skin, opt => opt.MapFrom(a => a.Skin))
            .ForMember(a => a.Badges, opt => opt.MapFrom(a => a.Badges))
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()));

        CreateMap<MinecraftAccount, MinecraftAccountDetailsDto>()
            .ForMember(a => a.PrimaryAccount, opt => opt.MapFrom(a => a.Selected))
            .ForMember(a => a.Badges, opt => opt.MapFrom(a => a.Badges))
            .ForMember(a => a.Skin, opt => opt.MapFrom(a => a.Skin));
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
        CreateMap<UserSettings, UserSettingsDto>()
            .ForMember(a => a.WeightStyle, opt => opt.MapFrom(a => a.WeightStyle))
            .ForMember(a => a.LeaderboardStyle, opt => opt.MapFrom(a => a.LeaderboardStyle))
            .ForMember(a => a.NameStyle, opt => opt.MapFrom(a => a.NameStyle))
            .ForMember(a => a.Fortune, opt => opt.MapFrom(a => a.Fortune));

        CreateMap<Entitlement, EntitlementDto>()
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()))
            .ForMember(a => a.Product, opt => opt.MapFrom(a => a.Product))
            .ForMember(a => a.ProductId, opt => opt.MapFrom(a => a.ProductId.ToString()));
        
        CreateMap<UserEntitlement, EntitlementDto>()
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()))
            .ForMember(a => a.Product, opt => opt.MapFrom(a => a.Product))
            .ForMember(a => a.ProductId, opt => opt.MapFrom(a => a.ProductId.ToString()));

        CreateMap<GuildEntitlement, EntitlementDto>()
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()))
            .ForMember(a => a.Product, opt => opt.MapFrom(a => a.Product))
            .ForMember(a => a.ProductId, opt => opt.MapFrom(a => a.ProductId.ToString()));
        
        CreateMap<ProductAccess, EntitlementDto>()
            .ForMember(a => a.Id, opt => opt.MapFrom(a => a.Id.ToString()))
            .ForMember(a => a.Product, opt => opt.MapFrom(a => a.Product))
            .ForMember(a => a.ProductId, opt => opt.MapFrom(a => a.ProductId.ToString()));

        CreateMap<Product, ProductDto>()
            .ForMember(p => p.Id, opt => opt.MapFrom(p => p.Id.ToString()))
            .ForMember(p => p.Features, opt => opt.MapFrom(p => p.Features))
            .ForMember(p => p.Images, opt => opt.MapFrom(p => p.Images))
            .ForMember(p => p.Thumbnail, opt => opt.MapFrom(p => p.Thumbnail))
            .ForMember(p => p.WeightStyles, opt => opt.MapFrom(p => p.WeightStyles))
            .ForMember(p => p.IsSubscription, opt => opt.MapFrom(p => p.IsGuildSubscription || p.IsUserSubscription))
            .ForMember(p => p.IsGuildSubscription, opt => opt.MapFrom(p => p.IsGuildSubscription))
            .ForMember(p => p.IsUserSubscription, opt => opt.MapFrom(p => p.IsUserSubscription));
        
        CreateMap<Product, ParentProductDto>()
            .ForMember(p => p.Id, opt => opt.MapFrom(p => p.Id.ToString()))
            .ForMember(p => p.Name, opt => opt.MapFrom(p => p.Name))
            .ForMember(p => p.Slug, opt => opt.MapFrom(p => p.Slug));
        
        CreateMap<UnlockedProductFeatures, UnlockedProductFeaturesDto>();
        CreateMap<ConfiguredProductFeatures, ConfiguredProductFeaturesDto>()
            .ForMember(c => c.WeightStyle, opt => opt.MapFrom<int?>(c => null));
    }
}