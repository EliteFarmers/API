using EliteAPI.Models.DTOs.Outgoing;
using Profile = AutoMapper.Profile;
using EliteAPI.Models.Entities.Hypixel;
using HypixelAPI.DTOs;

namespace EliteAPI.Mappers.ProfilesData;

public class PetMapper : Profile
{
    public PetMapper() {
        CreateMap<PetResponse, Pet>().ReverseMap();
        CreateMap<Pet, PetDto>().ReverseMap();
    }
}