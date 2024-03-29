using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Models.Entities.Farming;

[Table("FarmingWeights")] // Previous table name
public class Farming
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column(TypeName = "jsonb")]
    public FarmingInventory? Inventory { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public FarmingFortune? Fortune { get; set; } = new();
    
    public Pests Pests { get; set; } = new();
    
    public double TotalWeight { get; set; } = 0;
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> CropWeight { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> BonusWeight { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public Dictionary<Crop, long> UncountedCrops { get; set; } = new();

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}

[Owned]
public class Pests {
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

public class FarmingFortune {
    public int BaseCalculatedFortune { get; set; } = new();
    
    public Dictionary<Crop, int> CropCalculatedFortune { get; set; } = new();
    public Dictionary<string, int> GlobalFortuneSources { get; set; } = new();
    public Dictionary<string, int> SpecificFortuneSources { get; set; } = new();
}

public class FarmingInventory
{
    public List<ItemDto> Armor { get; set; } = new();
    public List<ItemDto> Tools { get; set; } = new();
    public List<ItemDto> Equipment { get; set; } = new();
    public List<ItemDto> Accessories { get; set; } = new();
}