using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class ProfileBankingMapper : Profile
{
    public ProfileBankingMapper()
    {
        CreateMap<ProfileBanking, ProfileBankingDto>()
            .ForMember(x => x.Balance, x => x.MapFrom(y => y.Balance));
    }
}

public class ProfileBankingTransactionMapper : Profile
{
    public ProfileBankingTransactionMapper()
    {
        CreateMap<ProfileBankingTransaction, ProfileBankingTransactionDto>();
    }
}