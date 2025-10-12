using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class SkillMapper : Profile {
	public SkillMapper() {
		CreateMap<Skills, SkillsDto>();
	}
}