using AutoMapper;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services;

namespace EliteAPI.Mappers.Leaderboards; 

public class LeaderboardMapper : Profile {
    public LeaderboardMapper() {
        CreateMap<LeaderboardEntry, LeaderboardEntryDto>();
        CreateMap<LeaderboardEntryWithRank, LeaderboardEntryWithRankDto>();
        CreateMap<Leaderboard, LeaderboardDto>();
    }
}