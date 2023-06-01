using EliteAPI.Data;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.ContestService;

public class ContestService : IContestService
{
    private readonly DataContext context;
    public ContestService(DataContext context)
    {
        this.context = context;
    }

    public async Task AddContest(JacobContest contest)
    {
        await context.JacobContests.AddAsync(contest);
        await context.SaveChangesAsync();
    }

    public async Task AddContestEvent(JacobContestEvent contestEvent)
    {
        await context.JacobContestEvents.AddAsync(contestEvent);
        await context.SaveChangesAsync();
    }

    public async Task AddContestParticipation(ContestParticipation entry)
    {
        await context.ContestParticipations.AddAsync(entry);
        await context.SaveChangesAsync();
    }

    public async Task<List<ContestParticipation>> GetAllConstestParticipations(string playerUUID, string profileId)
    {
        var participations = await context.ContestParticipations
            .Include(cp => cp.ProfileMember.MinecraftAccount)
            .Include(cp => cp.ProfileMember.Profile)
            .Where(cp =>
                cp.ProfileMember.MinecraftAccount.UUID == playerUUID &&
                cp.ProfileMember.Profile.ProfileId == profileId)
            .ToListAsync();

        return participations;
    }

    public async Task<JacobContestEvent?> GetContestEvent(DateTime timestamp)
    {
        return await context.JacobContestEvents.FirstOrDefaultAsync(contestEvent => contestEvent.Timestamp == timestamp);
    }

    public async Task<ContestParticipation?> GetContestParticipation(string playerUUID, string profileId, DateTime timestamp, Crop crop)
    {
        return await context.ContestParticipations
            .Include(entry => entry.ProfileMember.Profile)
            .Include(entry => entry.JacobContest)
            .FirstOrDefaultAsync(entry =>
                entry.ProfileMember.Profile.ProfileId == profileId &&
                entry.JacobContest.Crop == crop &&
                entry.JacobContest.Timestamp == timestamp);
    }

    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop)
    {
        throw new NotImplementedException();
    }

    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop, DateTime start, DateTime end)
    {
        throw new NotImplementedException();
    }

    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop, DateTime start, DateTime end, int limit)
    {
        throw new NotImplementedException();
    }

    public Task<List<ContestParticipation>> GetContestParticipationsByCrop(string playerUUID, string profileId, Crop crop, int limit)
    {
        throw new NotImplementedException();
    }
}
