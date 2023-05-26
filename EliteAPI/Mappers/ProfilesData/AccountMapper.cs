using AutoMapper;
using EliteAPI.Models;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Mappers.ProfilesData;

public class AccountMapper : Profile
{
    public AccountMapper()
    {
        CreateMap<Account, AccountDto>()
            .ForMember(a => a.DiscordAccount, opt => opt.MapFrom(a => a.DiscordAccount))
            .ForMember(a => a.PremiumUser, opt => opt.MapFrom(a => a.PremiumUser))
            .ForMember(a => a.MinecraftAccounts, opt => opt.MapFrom(a => a.MinecraftAccounts));
    }
}

public class MinecraftAccountMapper : Profile
{
    public MinecraftAccountMapper()
    {
        CreateMap<MinecraftAccount, MinecraftAccountDto>()
            .ForMember(a => a.Properties, opt => opt.MapFrom(a => a.Properties))
            .ForMember(a => a.Profiles, opt => opt.MapFrom(a => a.Profiles));
    }
}

public class DiscordAccountMapper : Profile
{
    public DiscordAccountMapper()
    {
        CreateMap<DiscordAccount, DiscordAccountDto>();
    }
}

public class MinecraftAccountPropertyMapper : Profile
{
    public MinecraftAccountPropertyMapper()
    {
        CreateMap<MinecraftAccountProperty, MinecraftAccountPropertyDto>();
    }
}