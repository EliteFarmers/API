using EliteAPI.Data;
using ErrorOr;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Articles;

interface IAnnouncementService
{
    Task CreateAnnouncementAsync(CreateAnnouncementDto dto, CancellationToken c = default);
    Task<List<AnnouncementDto>> GetAnnouncements(CancellationToken c);
    Task<ErrorOr<Success>> DismissAnnouncementAsync(Guid announcementId, ulong accountId, CancellationToken c = default);
}

[RegisterService<IAnnouncementService>(LifeTime.Scoped)]
public class AnnouncementService(DataContext context, IOutputCacheStore cacheStore): IAnnouncementService
{
    public async Task CreateAnnouncementAsync(CreateAnnouncementDto dto, CancellationToken c = default)
    {
        await context.Announcements.AddAsync(dto.ToModel(), c);
        await context.SaveChangesAsync(c);
        await cacheStore.EvictByTagAsync("announcements", c);
    }

    public async Task<List<AnnouncementDto>> GetAnnouncements(CancellationToken c)
    {
        return await context.Announcements
            .Where(a => a.ExpiresAt > DateTime.UtcNow)
            .Select(a => a.ToDto())
            .ToListAsync(c);
    }
    
    public async Task<ErrorOr<Success>> DismissAnnouncementAsync(Guid announcementId, ulong accountId, CancellationToken c = default)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == announcementId, c);
        
        if (announcement is null) {
            return Error.NotFound(description: "Announcement not found");
        }

        context.Announcements.Remove(announcement);
        await context.SaveChangesAsync(c);
        await cacheStore.EvictByTagAsync("announcements", c);
        
        return Result.Success;
    }
}