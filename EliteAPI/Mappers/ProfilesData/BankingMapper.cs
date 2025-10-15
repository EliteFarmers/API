using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class ProfileBankingMapper : Profile
{
	public ProfileBankingMapper() {
		CreateMap<ProfileBankingResponse, ProfileBanking>()
			.ForMember(x => x.Transactions, x => x.MapFrom(y => y.Transactions));

		CreateMap<ProfileBanking, ProfileBankingDto>()
			.ForMember(x => x.Balance, x => x.MapFrom(y => y.Balance));
	}
}

public class ProfileBankingTransactionMapper : Profile
{
	public ProfileBankingTransactionMapper() {
		CreateMap<RawTransaction, ProfileBankingTransaction>();

		CreateMap<ProfileBankingTransaction, ProfileBankingTransactionDto>();
	}
}