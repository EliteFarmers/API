using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Utilities;
using Profile = AutoMapper.Profile;

namespace EliteAPI.Mappers.Farming;

public class JacobDataMapper : Profile
{
    public JacobDataMapper() {
        CreateMap<JacobData, JacobDataDto>()
            .ForMember(j => j.Contests, opt => opt.MapFrom(x => x.Contests.OrderBy(c => c.JacobContestId)))
            .ForMember(j => j.EarnedMedals, opt => opt.MapFrom(x => x.EarnedMedals))
            .ForMember(j => j.Medals, opt => opt.MapFrom(x => x.Medals))
            .ForMember(j => j.Perks, opt => opt.MapFrom(x => x.Perks))
            .ForMember(j => j.Stats, opt => opt.MapFrom(x => x.Stats));

        CreateMap<JacobStats, JacobStatsDto>()
            .ForMember(j => j.Crops, opt => opt.MapFrom(x => x.Crops));

        CreateMap<JacobCropStats, JacobCropStatsDto>()
            .ForMember(j => j.Medals, opt => opt.MapFrom(x => x.Medals));
    }
}

public class JacobContestsMapper : Profile
{
    public JacobContestsMapper()
    {
        CreateMap<JacobContest, JacobContestDto>()
            .ForMember(j => j.Crop, opt => opt.MapFrom(x => FormatUtils.GetFormattedCropName(x.Crop)))
            .ForMember(j => j.Timestamp, opt => opt.MapFrom(x => x.Timestamp))
            .ForMember(j => j.Brackets, opt => opt.MapFrom(x => x.GetBrackets()))
            .ForMember(j => j.Participants, opt => opt.MapFrom(x => x.Participants));

        CreateMap<JacobContest, JacobContestWithParticipationsDto>()
            .ForMember(j => j.Crop, opt => opt.MapFrom(x => FormatUtils.GetFormattedCropName(x.Crop)))
            .ForMember(j => j.Timestamp, opt => opt.MapFrom(x => x.Timestamp))
            .ForMember(j => j.Brackets, opt => opt.MapFrom(x => x.GetBrackets()))
            .ForMember(j => j.Participants, opt => opt.MapFrom(x => x.Participants));
    }
}

public class JacobContestParticipationsMapper : Profile
{
    public JacobContestParticipationsMapper()
    {
        CreateMap<ContestParticipation, ContestParticipationDto>()
            .ForMember(j => j.Medal, opt => opt.MapFrom(x => FormatUtils.GetMedalName(x.MedalEarned)))
            .ForMember(j => j.Participants, opt => opt.MapFrom(x => x.JacobContest.Participants))
            .ForMember(j => j.Timestamp, opt => opt.MapFrom(x => x.JacobContest.Timestamp))
            .ForMember(j => j.Crop, opt => opt.MapFrom(x => FormatUtils.GetFormattedCropName(x.JacobContest.Crop)));

        CreateMap<ContestParticipation, StrippedContestParticipationDto>()
            .ForMember(j => j.Collected, opt => opt.MapFrom(x => x.Collected))
            .ForMember(j => j.Position, opt => opt.MapFrom(x => x.Position))
            .ForMember(j => j.Medal, opt => opt.MapFrom(x => FormatUtils.GetMedalName(x.MedalEarned)))
            .ForMember(j => j.Removed, opt => opt.MapFrom(x => x.ProfileMember.WasRemoved))
            .ForMember(j => j.PlayerUuid, opt => opt.MapFrom(x => x.ProfileMember.PlayerUuid))
            .ForMember(j => j.PlayerName, opt => opt.MapFrom(x => x.ProfileMember.MinecraftAccount.Name))
            .ForMember(j => j.ProfileUuid, opt => opt.MapFrom(x => x.ProfileMember.ProfileId));
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
        CreateMap<EarnedMedalInventory, EarnedMedalInventoryDto>();
    }
}

public class FarmingPestsMapper : Profile
{
    public FarmingPestsMapper()
    {
        CreateMap<Pests, PestsDto>();
    }
}