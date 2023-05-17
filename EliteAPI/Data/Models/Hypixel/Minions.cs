using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models.Hypixel;

public class CraftedMinion
{
    [Key] public int Id { get; set; }
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
        return (Tiers & (1 << tier)) != 0;
    }
}
