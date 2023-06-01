using EliteAPI.Models.DTOs.Outgoing;
using Profile = AutoMapper.Profile;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Mappers.ProfilesData;

public class CollectionMapper : Profile
{
    public CollectionMapper()
    {
        CreateMap<Collection, CollectionDto>()
            .ForMember(
                x => x.Name,
                x => x.MapFrom(y => y.Name))
            .ForMember(
                x => x.Amount,
                x => x.MapFrom(y => y.Amount));
    }
}

public class MinionMapper : Profile
{
    public MinionMapper()
    {
        CreateMap<CraftedMinion, CraftedMinionDto>();
    }
}

public class PetMapper : Profile
{
    public PetMapper()
    {
        CreateMap<Pet, PetDto>();
    }
}