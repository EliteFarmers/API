namespace EliteAPI.Models.DTOs.Outgoing;

public class FarmingWeightDto
{
    public double TotalWeight { get; set; } = 0;
    public Dictionary<string, double> CropWeight { get; set; } = new();
    public Dictionary<string, double> BonusWeight { get; set; } = new();
    
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

    public Dictionary<string, double> CropWeight { get; set; } = new();
    public Dictionary<string, double> BonusWeight { get; set; } = new();
}

public class FarmingInventoryDto
{
    public List<ItemDto> Armor { get; set; } = new();
    public List<ItemDto> Tools { get; set; } = new();
    public List<ItemDto> Equipment { get; set; } = new();
}