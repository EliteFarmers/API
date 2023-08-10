using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.TimescaleService; 

public class TimescaleService : ITimescaleService {
    private readonly DataContext _context;

    public TimescaleService(DataContext context) {
        _context = context;
    }

    public async Task<List<CollectionDataPointDto>> GetCropCollection(Guid memberId, Crop crop, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        // Get all crop collections for the member between the start and end time. 
        var cropCollection = await _context.CropCollections
            .Where(cc => cc.ProfileMemberId == memberId && cc.Time >= start && cc.Time <= end)
            .OrderBy(cc => cc.Time)
            .ToListAsync();
        
        
        var groupedByDate = cropCollection.GroupBy(obj => obj.Time.Date);

        var selectedEntries = new List<CollectionDataPointDto>();

        foreach (var group in groupedByDate)
        {
            // Order the entries in the group by their actual DateTimeOffset values
            var orderedGroup = group.OrderBy(obj => obj.Time);

            // Calculate the time interval between entries
            var maxInterval = TimeSpan.FromHours(24) / perDay; // Divide the day into x intervals

            DateTimeOffset? previousTime = null;
            foreach (var entry in orderedGroup)
            {
                if (!previousTime.HasValue || (entry.Time - previousTime.Value) >= maxInterval)
                {
                    selectedEntries.Add(new CollectionDataPointDto {
                        Timestamp = entry.Time.ToUnixTimeSeconds(),
                        Value = entry.NetherWart
                    });
                    previousTime = entry.Time;
                }

                if (selectedEntries.Count >= 4) break;
            }
        }

        return selectedEntries;
    }

    public async Task<List<CropCollectionsDataPointDto>> GetCropCollections(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        // Get all crop collections for the member between the start and end time. 
        var cropCollection = await _context.CropCollections
            .Where(cc => cc.ProfileMemberId == memberId && cc.Time >= start && cc.Time <= end)
            .OrderBy(cc => cc.Time)
            .ToListAsync();
        
        
        var groupedByDate = cropCollection.GroupBy(obj => obj.Time.Date);

        var selectedEntries = new List<CropCollectionsDataPointDto>();

        foreach (var group in groupedByDate)
        {
            // Order the entries in the group by their actual DateTimeOffset values
            var orderedGroup = group.OrderBy(obj => obj.Time);

            // Calculate the time interval between entries
            var maxInterval = TimeSpan.FromHours(24) / perDay; // Divide the day into x intervals

            DateTimeOffset? previousTime = null;
            foreach (var entry in orderedGroup)
            {
                if (!previousTime.HasValue || (entry.Time - previousTime.Value) >= maxInterval)
                {
                    selectedEntries.Add(new CropCollectionsDataPointDto {
                        Timestamp = entry.Time.ToUnixTimeSeconds(),
                        Crops = entry.ExtractReadableCropCollections()
                    });
                    previousTime = entry.Time;
                }

                if (selectedEntries.Count >= 4) break;
            }
        }

        return selectedEntries;
    }

    public async Task<List<SkillDataPointDto>> GetSkill(Guid memberId, string skill, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        throw new NotImplementedException();
    }

    public async Task<List<SkillsDataPointDto>> GetSkills(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4) {
        throw new NotImplementedException();
    }
}