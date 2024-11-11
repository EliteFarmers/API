using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Outgoing;

public class FarmingWeightDto
{
    public double TotalWeight { get; set; } = 0;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, long>? Crops { get; set; }
    public Dictionary<string, double> CropWeight { get; set; } = new();
    public Dictionary<string, double> BonusWeight { get; set; } = new();
    
    public Dictionary<string, int> UncountedCrops { get; set; } = new();
    
    public PestsDto Pests { get; set; } = new();
    
    public FarmingInventoryDto? Inventory { get; set; } = new();
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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, long>? Crops { get; set; }
    public Dictionary<string, double> CropWeight { get; set; } = new();
    public Dictionary<string, double> BonusWeight { get; set; } = new();
        
    public Dictionary<string, int> UncountedCrops { get; set; } = new();
    
    public PestsDto Pests { get; set; } = new();
}

public class FarmingInventoryDto
{
    public List<ItemDto> Armor { get; set; } = new();
    public List<ItemDto> Tools { get; set; } = new();
    public List<ItemDto> Equipment { get; set; } = new();
    public List<ItemDto> Accessories { get; set; } = new();
}

public class PestsDto {
    public int Beetle { get; set; } = 0;
    public int Cricket { get; set; } = 0;
    public int Fly { get; set; } = 0;
    public int Locust { get; set; } = 0;
    public int Mite { get; set; } = 0;
    public int Mosquito { get; set; } = 0;
    public int Moth { get; set; } = 0;
    public int Rat { get; set; } = 0;
    public int Slug { get; set; } = 0;
    public int Earthworm { get; set; } = 0;
}

public class WeightsDto {
    public Dictionary<string, double> Crops { get; set; } = new();
    public PestWeightsDto Pests { get; set; } = new();
}

public class PestWeightsDto {
    public Dictionary<string, int> Brackets { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> Values { get; set; } = new();
}
