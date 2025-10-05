using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Profiles.Models;

public class HypixelInventory
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), MapperIgnore]
	public long HypixelInventoryId { get; set; }
	
	[MaxLength(64)]
	public required string Name { get; set; }
	
	[MapperIgnore]
	public Guid ProfileMemberId { get; set; }
	
	public List<HypixelItem> Items { get; set; } = [];
	
	[Column(TypeName = "jsonb")]
	public Dictionary<string, string>? Metadata { get; set; } 
}

public class HypixelInventoryConfiguration : IEntityTypeConfiguration<HypixelInventory>
{
	public void Configure(EntityTypeBuilder<HypixelInventory> builder)
	{
		builder.HasMany<HypixelItem>(i => i.Items)
			.WithOne()
			.HasForeignKey(i => i.InventoryId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany<HypixelItem>(i => i.Items);
		
		builder.HasOne<ProfileMember>()
			.WithMany(pm => pm.Inventories)
			.HasForeignKey(i => i.ProfileMemberId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}