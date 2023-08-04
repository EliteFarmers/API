using EliteAPI.Models.Entities.Hypixel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
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
    public List<object> Armor { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public List<object> Tools { get; set; } = new();
    
    [Column(TypeName = "jsonb")]
    public List<object> Equipment { get; set; } = new();
}

public class FarmingTool {
    public string? Name { get; set; }
    public Crop Crop { get; set; }
    
    public string? Id { get; set; }
    public string? Uuid { get; set; }
    
    public long Cultivating { get; set; }
    public long Counter { get; set; }
    
    public Dictionary<string, int> Enchantments { get; set; } = new();
}

