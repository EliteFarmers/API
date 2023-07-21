using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.LeaderboardService;

namespace EliteAPI.Mappers.Leaderboards; 

public class LeaderboardMapper : Profile {
    public LeaderboardMapper() {
        CreateMap<LeaderboardEntry, LeaderboardEntryDto>();
        CreateMap<LeaderboardEntryWithRank, LeaderboardEntryWithRankDto>();
        CreateMap<Leaderboard, LeaderboardDto>();
    }
}