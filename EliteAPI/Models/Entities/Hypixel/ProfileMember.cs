using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Models.Entities.Hypixel;

public class ProfileMember
{
	[Key] public required Guid Id { get; set; }

	public ApiAccess Api { get; set; } = new();

	public int SkyblockXp { get; set; } = 0;
	public double Purse { get; set; } = 0;

	public JacobData JacobData { get; set; } = new();
	public Skills Skills { get; set; } = new();
	public Farming.Farming Farming { get; set; } = new();
	public ChocolateFactory ChocolateFactory { get; set; } = new();

	public ProfileMemberMetadata? Metadata { get; set; }

	// public Inventories Inventories { get; set; } = new(); // Likely to be added in the future
	public List<HypixelInventory> Inventories { get; set; } = [];
	public bool IsSelected { get; set; } = false;
	public bool WasRemoved { get; set; } = false;
	public long LastUpdated { get; set; } = 0;

	[Column(TypeName = "jsonb")] public Dictionary<string, long> Collections { get; set; } = new();

	[Column(TypeName = "jsonb")] public UnparsedApiData Unparsed { get; set; } = new();

	[Column(TypeName = "jsonb")] public Dictionary<string, int> CollectionTiers { get; set; } = new();
	[Column(TypeName = "jsonb")] public Dictionary<string, long> Sacks { get; set; } = new();
	[Column(TypeName = "jsonb")] public Slayers? Slayers { get; set; }
	[Column(TypeName = "jsonb")] public List<Pet> Pets { get; set; } = new();

	[ForeignKey("MinecraftAccount")] public required string PlayerUuid { get; set; }
	public MinecraftAccount MinecraftAccount { get; set; } = null!;

	[ForeignKey("Profile")] public required string ProfileId { get; set; }
	public required Profile Profile { get; set; }
	public string? ProfileName { get; set; }

	public List<EventMember> EventEntries { get; set; } = [];
	public List<EndedAuction> EndedAuctions { get; set; } = [];
}

[Owned]
public class ApiAccess
{
	public bool Inventories { get; set; } = false;
	public bool Collections { get; set; } = false;
	public bool Skills { get; set; } = false;
	public bool Vault { get; set; } = false;
	public bool Museum { get; set; } = false;
}