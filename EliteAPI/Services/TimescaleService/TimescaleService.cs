using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.TimescaleService; 

public class TimescaleService : ITimescaleService {
    private readonly DataContext _context;

    public TimescaleService(DataContext context) {
        _context = context;
    }

    public async Task<List<CollectionDataPointDto>> GetCropCollection(Guid memberId, Crop crop, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        throw new NotImplementedException();
    }

    public async Task<List<CropCollectionsDataPointDto>> GetCropCollections(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        // Get all crop collections for the member between the start and end time. 
        var cropCollection = await _context.CropCollections
            .Where(cc => cc.ProfileMemberId == memberId && cc.Time >= start && cc.Time <= end)
            .OrderBy(cc => cc.Time)
            .ToListAsync();
        
        var groupedByDay = cropCollection.GroupBy(obj => obj.Time.DayOfYear);

        var selectedEntries = new List<CropCollection>();

        foreach (var group in groupedByDay)
        {
            // Order the entries in the group by their actual time values
            var orderedGroup = group.OrderBy(obj => obj.Time).ToList();

            // Add all entries if there are less or equal than asked for
            if (orderedGroup.Count <= perDay) {
                selectedEntries.AddRange(orderedGroup);
                continue;
            }

            // Group the entries by the time interval
            var span = TimeSpan.FromDays(1) / perDay;
            var groupedByInterval = orderedGroup.GroupBy(obj => obj.Time.Ticks / span.Ticks).ToList();
            
            // Add the first entry of each interval
            selectedEntries.AddRange(groupedByInterval.Select(hourly => hourly.First()));

            // If there are enough groups, we are done here
            if (groupedByInterval.Count >= perDay) continue;
 
            var intervals = groupedByInterval
                .Select(g => g.ToList())
                .Where(g => g.Count > 1).ToList();
            
            // Won't guarantee that that the max amount of entries will be returned, and might go over, but it's close enough
            selectedEntries.AddRange(intervals.Select(g => g.Last()));
        }

        return selectedEntries.Select(c => new CropCollectionsDataPointDto {
            Crops = c.ExtractReadableCropCollections(),
            Timestamp = c.Time.ToUnixTimeSeconds()
        }).ToList();
    }

    public async Task<List<SkillDataPointDto>> GetSkill(Guid memberId, string skill, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        throw new NotImplementedException();
    }

    public async Task<List<SkillsDataPointDto>> GetSkills(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        throw new NotImplementedException();
    }
}