using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.Models;

[Index(nameof(Name), Name = "idx_minecraft_accounts_name")]
public class MinecraftAccount
{
	[Key] public required string Id { get; set; }
	public required string Name { get; set; }

	[ForeignKey("EliteAccount")] public ulong? AccountId { get; set; }
	public EliteAccount? EliteAccount { get; set; }

	public bool Selected { get; set; }

	public PlayerData? PlayerData { get; set; }

	[MaxLength(128)] public string? TextureId { get; set; }

	[Column(TypeName = "bytea")] public byte[]? Face { get; set; }
	[Column(TypeName = "bytea")] public byte[]? Hat { get; set; }

	public string? FaceUrl => Face != null ? $"data:image/png;base64,{Convert.ToBase64String(Face)}" : null;
	public string? HatUrl => Hat != null ? $"data:image/png;base64,{Convert.ToBase64String(Hat)}" : null;

	public MinecraftSkinDto Skin => new() {
		Face = FaceUrl,
		Hat = HatUrl,
		Texture = TextureId
	};

	public AccountFlag Flags { get; set; } = AccountFlag.None;
	public bool IsBanned => Flags.HasFlag(AccountFlag.Banned);

	[Column(TypeName = "jsonb")] public List<FlagReason>? FlagReasons { get; set; }

	public List<UserBadge> Badges { get; set; } = null!;

	public long LastUpdated { get; set; }
	public long ProfilesLastUpdated { get; set; }
	public long PlayerDataLastUpdated { get; set; }
}

public class MinecraftAccountProperty
{
	public required string Name { get; set; }
	public required string Value { get; set; }
}

public enum AccountFlag : ushort
{
	None = 0,
	AutoFlag = 1,
	Banned = 2
}

public class FlagReason
{
	public AccountFlag Flag { get; set; }
	public string Reason { get; set; } = string.Empty;
	public string Proof { get; set; } = string.Empty;
	public long Timestamp { get; set; }
}