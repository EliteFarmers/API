using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Utilities;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.ProfilesData;

public class JacobDataMapper : Profile
{
    public JacobDataMapper()
    {
        CreateMap<JacobData, JacobDataDto>()
            .ForMember(j => j.Contests, opt => opt.MapFrom(x => x.Contests))
            .ForMember(j => j.EarnedMedals, opt => opt.MapFrom(x => x.EarnedMedals))
            .ForMember(j => j.Medals, opt => opt.MapFrom(x => x.Medals))
            .ForMember(j => j.Perks, opt => opt.MapFrom(x => x.Perks));
    }
}

public class JacobContestsMapper : Profile
{
    public JacobContestsMapper()
    {
        CreateMap<JacobContest, JacobContestDto>()
            .ForMember(j => j.Participations, opt => opt.MapFrom(x => x.Participations));
    }
}

public class JacobContestEventsMapper : Profile
{
    public JacobContestEventsMapper()
    {
        CreateMap<JacobContestEvent, JacobContestEventDto>()
            .ForMember(j => j.JacobContests, opt => opt.MapFrom(x => x.JacobContests));
    }
}

public class JacobContestParticipationsMapper : Profile
{
    public JacobContestParticipationsMapper()
    {
        CreateMap<ContestParticipation, ContestParticipationDto>()
            .ForMember(j => j.MedalEarned, opt => opt.MapFrom(x => x.MedalEarned))
            //.ForMember(j => j.Participants, opt => opt.MapFrom(x => x.JacobContest.Participants))
            //.ForMember(j => j.Timestamp, opt => opt.MapFrom(x => x.JacobContest.Timestamp))
            .ForMember(j => j.Crop, opt => opt.MapFrom(x => FormatUtils.GetFormattedCropName(x.Crop)));
    }
}

public class JacobPerksMapper : Profile
{
    public JacobPerksMapper()
    {
        CreateMap<JacobPerks, JacobPerksDto>();
    }
}

public class MedalInventoryMapper : Profile
{
    public MedalInventoryMapper()
    {
        CreateMap<MedalInventory, MedalInventoryDto>();
    }
}