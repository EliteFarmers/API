using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Models.Entities.Timescale; 

[Keyless]
public class CropCollection {
    public long Wheat { get; set; }
    public long Carrot { get; set; }
    public long Potato { get; set; }
    public long Pumpkin { get; set; }
    public long Melon { get; set; }
    public long Mushroom { get; set; }
    public long CocoaBeans { get; set; }
    public long Cactus { get; set; }
    public long SugarCane { get; set; }
    public long NetherWart { get; set; }
    public long Seeds { get; set; }
    
    [HypertableColumn]
    public DateTimeOffset Time { get; set; }

    [ForeignKey("Member")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember ProfileMember { get; set; } = null!;
}