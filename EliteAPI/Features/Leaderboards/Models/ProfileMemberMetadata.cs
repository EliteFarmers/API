using System.Text.Json.Serialization;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Leaderboards.Models;

public class ProfileMemberMetadata {
	public Guid ProfileMemberId { get; set; }
	public ProfileMember ProfileMember { get; set; } = null!;
	
	public required string Name { get; set; }
	public required string Uuid { get; set; }
	/// <summary>
	/// Prefix of the player
	/// </summary>
	public string? Prefix { get; set; }
	public required string Profile { get; set; }
	public required string ProfileUuid { get; set; }
	/// <summary>
	/// Skyblock experience used to order members within a profile leaderboard
	/// </summary>
	public int SkyblockExperience { get; set; }
	
	public ProfileMemberMetadataCosmetics? Cosmetics { get; set; }
}


public class ProfileMemberMetadataConfiguration : IEntityTypeConfiguration<ProfileMemberMetadata>
{
	public void Configure(EntityTypeBuilder<ProfileMemberMetadata> builder)
	{
		builder.HasKey(m => m.ProfileMemberId);
		
		builder.HasOne(m => m.ProfileMember)
			.WithOne(p => p.Metadata)
			.HasForeignKey<ProfileMemberMetadata>(m => m.ProfileMemberId)
			.OnDelete(DeleteBehavior.Cascade);
		
		builder.OwnsOne(m => m.Cosmetics, ownedNavigation => {
			ownedNavigation.ToJson();
			ownedNavigation.OwnsOne(m => m.Leaderboard);
		});
	}
}

public class ProfileMemberMetadataCosmetics {
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ProfileMemberLeaderboardCosmetics? Leaderboard { get; set; } 
}

public class ProfileMemberLeaderboardCosmetics {
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? BackgroundColor { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? BorderColor { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TextColor { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RankColor { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? BackgroundImage { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OverlayImage { get; set; }
}