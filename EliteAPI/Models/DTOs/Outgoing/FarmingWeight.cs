using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.DTOs.Outgoing;

public class FarmingWeightDto
{
    public double TotalWeight { get; set; } = 0;

    public Dictionary<string, double> CropWeight { get; set; } = new();
    public Dictionary<string, double> BonusWeight { get; set; } = new();
}

public class FarmingWeightAllProfilesDto {
    public string? SelectedProfileId { get; set; }
    public List<FarmingWeightWithProfileDto> Profiles { get; set; } = new();
}

public class FarmingWeightWithProfileDto
{
    public required string ProfileId { get; set; }
    public required string ProfileName { get; set; }

    public double TotalWeight { get; set; } = 0;

    public Dictionary<string, double> CropWeight { get; set; } = new();
    public Dictionary<string, double> BonusWeight { get; set; } = new();
}

public class FarmingInventoryDto
{
    public int BaseCalculatedFortune { get; set; } = new();
    public Dictionary<Crop, int> CropCalculatedFortune { get; set; } = new();

    public List<object> Armor { get; set; } = new();
    public List<object> Tools { get; set; } = new();
    public List<object> Equipment { get; set; } = new();

    public Dictionary<string, int> GlobalFortuneSources { get; set; } = new();
    public Dictionary<string, int> SpecificFortuneSources { get; set; } = new();
}