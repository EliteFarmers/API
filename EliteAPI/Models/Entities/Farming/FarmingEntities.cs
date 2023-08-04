using EliteAPI.Models.Entities.Hypixel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using EliteAPI.Models.DTOs.Outgoing;
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
    
    public double TotalWeight { get; set; } = 0;
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> CropWeight { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> BonusWeight { get; set; } = new();

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}

public class FarmingFortune {
    public int BaseCalculatedFortune { get; set; } = new();
    
    public Dictionary<Crop, int> CropCalculatedFortune { get; set; } = new();
    public Dictionary<string, int> GlobalFortuneSources { get; set; } = new();
    public Dictionary<string, int> SpecificFortuneSources { get; set; } = new();
}

public class FarmingInventory
{
    [Column(TypeName = "jsonb")]
    public List<ItemDto> Armor { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public List<ItemDto> Tools { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public List<ItemDto> Equipment { get; set; } = new();
}