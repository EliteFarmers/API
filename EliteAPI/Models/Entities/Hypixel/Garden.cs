using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Models.Entities.Hypixel;

[Flags]
public enum UnlockedPlots : uint {
	None = 0,
	
	Beginner1 = 0b_0000_0000_0000_0000_0000_0001,
	Beginner2 = 0b_0000_0000_0000_0000_0000_0010,
	Beginner3 = 0b_0000_0000_0000_0000_0000_0100,
	Beginner4 = 0b_0000_0000_0000_0000_0000_1000,
	Beginner = Beginner1 | Beginner2 | Beginner3 | Beginner4,
	
	Intermediate1 = 0b_0000_0000_0000_0000_0001_0000,
	Intermediate2 = 0b_0000_0000_0000_0000_0010_0000,
	Intermediate3 = 0b_0000_0000_0000_0000_0100_0000,
	Intermediate4 = 0b_0000_0000_0000_0000_1000_0000,
	Intermediate = Intermediate1 | Intermediate2 | Intermediate3 | Intermediate4,
	
	Advanced1 = 0b_0000_0000_0000_0001_0000_0000,
	Advanced2 = 0b_0000_0000_0000_0010_0000_0000,
	Advanced3 = 0b_0000_0000_0000_0100_0000_0000,
	Advanced4 = 0b_0000_0000_0000_1000_0000_0000,
	Advanced5 = 0b_0000_0000_0001_0000_0000_0000,
	Advanced6 = 0b_0000_0000_0010_0000_0000_0000,
	Advanced7 = 0b_0000_0000_0100_0000_0000_0000,
	Advanced8 = 0b_0000_0000_1000_0000_0000_0000,
	Advanced9 = 0b_0000_0001_0000_0000_0000_0000,
	Advanced10 = 0b_0000_0010_0000_0000_0000_0000,
	Advanced11 = 0b_0000_0100_0000_0000_0000_0000,
	Advanced12 = 0b_0000_1000_0000_0000_0000_0000,
	Advanced = Advanced1 | Advanced2 | Advanced3 | Advanced4 | Advanced5 | Advanced6 | Advanced7 | Advanced8 | Advanced9 | Advanced10 | Advanced11 | Advanced12,
	
	Expert1 = 0b_0001_0000_0000_0000_0000_0000,
	Expert2 = 0b_0010_0000_0000_0000_0000_0000,
	Expert3 = 0b_0100_0000_0000_0000_0000_0000,
	Expert4 = 0b_1000_0000_0000_0000_0000_0000,
	Expert = Expert1 | Expert2 | Expert3 | Expert4,
	
	All = Beginner | Intermediate | Advanced | Expert
}

public class Garden {
	[Key, ForeignKey("Profile"), MaxLength(36)]
	public required string ProfileId { get; set; }
	public Profile Profile { get; set; } = null!;
	
	public long GardenExperience { get; set; } = 0;
	
	public int CompletedVisitors { get; set; } = 0;
	public int UniqueVisitors { get; set; } = 0;
	
	public MilestoneCrops Crops { get; set; } = new();
	public CropUpgrades Upgrades { get; set; } = new();
	
	public UnlockedPlots UnlockedPlots { get; set; } = 0; 

	[Column(TypeName = "jsonb")]
	public ComposterData? Composter { get; set; }
	
	[Column(TypeName = "jsonb")]
	public Dictionary<string, VisitorData> Visitors { get; set; } = new();
	
	public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

[Owned]
public class MilestoneCrops {
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
}

[Owned]
public class CropUpgrades {
	public short Wheat { get; set; }
	public short Carrot { get; set; }
	public short Potato { get; set; }
	public short Pumpkin { get; set; }
	public short Melon { get; set; }
	public short Mushroom { get; set; }
	public short CocoaBeans { get; set; }
	public short Cactus { get; set; }
	public short SugarCane { get; set; }
	public short NetherWart { get; set; }
}

public class VisitorData {
	public int Visits { get; set; }
	public int Accepted { get; set; }
}