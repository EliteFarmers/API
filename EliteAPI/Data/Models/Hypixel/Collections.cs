using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EliteAPI.Data.Models.Hypixel;

public class Collection
{
    [Key] public int Id { get; set; }
    public required string Name { get; set; }
    public required long Amount { get; set; }
    public int Tier { get; set; }

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public required ProfileMember ProfileMember { get; set; }
}