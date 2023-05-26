using EliteAPI.Models.Hypixel;

namespace EliteAPI.Services.ContestService;

public interface IContestService
{
    public Task AddContest(JacobContest contest);
    public Task AddContestEvent(JacobContestEvent contestEvent);
    public Task AddContestParticipation(ContestParticipation entry);
    public Task<JacobContestEvent?> GetContestEvent(DateTime timestamp);
    public Task<ContestParticipation?> GetContestParticipation(string playerUUID, string profileId, DateTime timestamp, Crop crop);
    public Task<List<ContestParticipation>> GetAllConstestParticipations(string playerUUID, string profileId);
    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop);
    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop, DateTime start, DateTime end);
    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop, DateTime start, DateTime end, int limit);
    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop, int limit);
}
