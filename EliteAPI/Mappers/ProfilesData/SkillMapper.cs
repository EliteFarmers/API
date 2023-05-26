using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Hypixel;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class SkillMapper : Profile
{
    public SkillMapper()
    {
        CreateMap<Skill, SkillDto>();
    }
}