using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using Profile = AutoMapper.Profile;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Mappers.ProfilesData;

public class PetMapper : Profile
{
    public PetMapper()
    {
        CreateMap<RawPetData, Pet>();

        CreateMap<Pet, PetDto>();
    }
}