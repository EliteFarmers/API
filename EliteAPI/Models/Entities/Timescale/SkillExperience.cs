using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Models.Entities.Timescale; 

[Keyless]
public class SkillExperience : ITimeScale {
    public double Combat { get; set; } 
    public double Mining { get; set; }
    public double Foraging { get; set; }
    public double Fishing { get; set; }
    public double Enchanting { get; set; }
    public double Alchemy { get; set; }
    public double Carpentry { get; set; }
    public double Runecrafting { get; set; }
    public double Social { get; set; }
    public double Taming { get; set; }
    public double Farming { get; set; }
    
    [HypertableColumn]
    public DateTimeOffset Time { get; set; }

    [ForeignKey("Member")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember ProfileMember { get; set; } = null!;
}