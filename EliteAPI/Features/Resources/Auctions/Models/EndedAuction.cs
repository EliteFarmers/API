using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Auctions.Models;

public class EndedAuction
{
	[Key]
	public Guid AuctionId { get; set; }
	
	public Guid SellerUuid { get; set; }
	public Guid SellerProfileUuid { get; set; }
	
	[MapperIgnore]
	public Guid? SellerProfileMemberId { get; set; }
	
	public Guid BuyerUuid { get; set; }
	public Guid BuyerProfileUuid { get; set; }
	
	[MapperIgnore]
	public Guid? BuyerProfileMemberId { get; set; }
	
	public long Timestamp { get; set; }
	public long Price { get; set; }
	public short Count { get; set; }
	public bool Bin { get; set; }
	public Guid? ItemUuid { get; set; }
	
	[MaxLength(256)]
	public string? SkyblockId { get; set; }
	[MaxLength(512)]
	public string VariantKey { get; set; } = string.Empty;
	public required byte[] Item { get; set; }
}

public class EndedAuctionWorkItem
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }
	
	public required Guid AuctionId { get; set; }
	public required EndedAuction Auction { get; set; }
}

public class EndedAuctionConfiguration : IEntityTypeConfiguration<EndedAuction>
{
	public void Configure(EntityTypeBuilder<EndedAuction> builder) {
		builder.HasIndex(e => e.Price);
		builder.HasIndex(e => e.Timestamp);
		builder.HasIndex(e => new { e.SkyblockId, e.Timestamp });
		builder.HasIndex(e => new { e.SkyblockId, e.VariantKey, e.Timestamp });
		builder.HasIndex(e => e.SellerProfileMemberId);
		builder.HasIndex(e => e.BuyerProfileMemberId);
		builder.Property(e => e.VariantKey).HasDefaultValue(string.Empty);
		
		builder.HasOne<ProfileMember>()
			.WithMany(p => p.EndedAuctions)
			.HasForeignKey(e => e.SellerProfileMemberId);
		
		builder.HasOne<ProfileMember>()
			.WithMany()
			.HasForeignKey(e => e.BuyerProfileMemberId);
	}
}