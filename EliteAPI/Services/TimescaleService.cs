using System.Globalization;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services; 

public class TimescaleService(DataContext context) : ITimescaleService 
{
    public async Task<List<CollectionDataPointDto>> GetCropCollection(Guid memberId, Crop crop, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        var crops = await GetCropCollections(memberId, start, end, perDay);
        
        return crops.Select(c => new CollectionDataPointDto {
            Value = c.Crops[crop.SimpleName()],
            Timestamp = c.Timestamp
        }).ToList();
    }

    public async Task<List<CropCollectionsDataPointDto>> GetCropCollections(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        // Get all crop collections for the member between the start and end time. 
        var cropCollection = await context.CropCollections
            .Where(cc => cc.ProfileMemberId == memberId && cc.Time >= start && cc.Time <= end)
            .OrderBy(cc => cc.Time)
            .ToListAsync();

        var selectedEntries = MaxPerDayFilter(cropCollection, perDay);
        
        return selectedEntries.Select(c => new CropCollectionsDataPointDto {
            Crops = c.ExtractReadableCropCollections(),
            Pests = c.ExtractPestKills(),
            CropWeights = c.CountCropWeight().ToString(CultureInfo.InvariantCulture),
            Timestamp = c.Time.ToUnixTimeSeconds()
        }).ToList();
    }

    public Task<List<SkillDataPointDto>> GetSkill(Guid memberId, string skill, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        throw new NotImplementedException();
    }

    public async Task<List<SkillsDataPointDto>> GetSkills(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        // Get all crop collections for the member between the start and end time. 
        var skillExperiences = await context.SkillExperiences
            .Where(cc => cc.ProfileMemberId == memberId && cc.Time >= start && cc.Time <= end)
            .OrderBy(cc => cc.Time)
            .ToListAsync();

        var selectedEntries = MaxPerDayFilter(skillExperiences, perDay);
        
        return selectedEntries.Select(c => new SkillsDataPointDto {
            Skills = c.ExtractSkills(),
            Timestamp = c.Time.ToUnixTimeSeconds()
        }).ToList();
    }
    
    private static List<T> MaxPerDayFilter<T>(IEnumerable<T> collection, int perDay = 4) where T : ITimeScale {
        if (perDay == -1) return collection.ToList();
        
        var groupedByDay = collection.GroupBy(obj => obj.Time.DayOfYear);

        var selectedEntries = new List<T>();

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
            
            // Add the last entry of each interval
            selectedEntries.AddRange(groupedByInterval.Select(hourly => hourly.Last()));

            // If there are enough groups, we are done here
            if (groupedByInterval.Count >= perDay) continue;
 
            var intervals = groupedByInterval
                .Select(g => g.ToList())
                .Where(g => g.Count > 1).ToList();
            
            // Won't guarantee that the max amount of entries will be returned, and might go over, but it's close enough
            selectedEntries.AddRange(intervals.Select(g => g.First()));
        }
        
        return selectedEntries;
    }
}