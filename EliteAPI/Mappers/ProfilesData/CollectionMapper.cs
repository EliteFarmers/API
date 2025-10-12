using EliteAPI.Models.DTOs.Outgoing;
using Profile = AutoMapper.Profile;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Inventories;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Mappers.ProfilesData;

public class PetMapper : Profile {
	public PetMapper() {
		CreateMap<PetResponse, Pet>()
			.ForMember(p => p.Level, opt => opt.MapFrom(src => src.GetLevel()));

		CreateMap<Pet, PetDto>().ReverseMap();
	}
}