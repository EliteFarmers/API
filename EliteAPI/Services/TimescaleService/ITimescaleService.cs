using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Services.TimescaleService; 

public interface ITimescaleService {
    Task<List<CollectionDataPointDto>> GetCropCollection(Guid memberId, Crop crop, DateTimeOffset start, DateTimeOffset end, int perDay = 4);
    Task<List<CropCollectionsDataPointDto>> GetCropCollections(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4);
    Task<List<SkillDataPointDto>> GetSkill(Guid memberId, string skill, DateTimeOffset start, DateTimeOffset end, int perDay = 4);
    Task<List<SkillsDataPointDto>> GetSkills(Guid memberId, DateTimeOffset start, DateTimeOffset end, int perDay = 4);
}