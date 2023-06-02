using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EliteAPI.Models.Entities.Hypixel;

public class Collection
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required string Name { get; set; }
    public required long Amount { get; set; }
    public int Tier { get; set; }

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public required ProfileMember ProfileMember { get; set; }
}

public class CraftedMinion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Type { get; set; }
    public int Tiers { get; set; } = 0;

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }

    public void RegisterTier(int tier)
    {
        // Bit shift the tier into the correct position of the tiers int
        Tiers |= 1 << tier;
    }

    public bool IsTierRegistered(int tier)
    {
        // Bit shift the tier into the correct position of the tiers int
        return (Tiers & 1 << tier) != 0;
    }
}

public class Pet
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Uuid { get; set; }
    public string? Type { get; set; }
    public double Exp { get; set; } = 0;
    public bool Active { get; set; } = false;
    public string? Tier { get; set; }
    public string? HeldItem { get; set; }
    public short CandyUsed { get; set; } = 0;
    public string? Skin { get; set; }

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}
