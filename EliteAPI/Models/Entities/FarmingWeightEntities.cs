using EliteAPI.Models.Entities.Hypixel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EliteAPI.Models.Entities;

public class FarmingWeight
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public double TotalWeight { get; set; } = 0;
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> CropWeight { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> BonusWeight { get; set; } = new();

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}

/*
public class FarmingInventory
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int BaseCalculatedFortune { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<Crop, int> CropCalculatedFortune { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public List<object> Armor { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public List<object> Tools { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public List<object> Equipment { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public Dictionary<string, int> GlobalFortuneSources { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public Dictionary<string, int> SpecificFortuneSources { get; set; } = new();

    public long LastUpdated { get; set; } = 0;

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}*/