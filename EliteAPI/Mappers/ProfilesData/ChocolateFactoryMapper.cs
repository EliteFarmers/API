using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class ChocolateFactoryMapper : Profile
{
	public ChocolateFactoryMapper() {
		CreateMap<ChocolateFactory, ChocolateFactoryDto>()
			.ForMember(c => c.LastViewed, opt => opt.MapFrom(c => c.LastViewedChocolateFactory));
		CreateMap<ChocolateFactoryRabbits, ChocolateFactoryRabbitsDto>();
	}
}