using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Auctions.Models;

[Table("EndedAuctions")]
public class Auction
{
	[Key]
	public Guid AuctionId { get; set; }
	
	public Guid SellerUuid { get; set; }
	public Guid SellerProfileUuid { get; set; }
	
	[MapperIgnore]
	public Guid? SellerProfileMemberId { get; set; }
	
	public Guid? BuyerUuid { get; set; }
	public Guid? BuyerProfileUuid { get; set; }
	
	[MapperIgnore]
	public Guid? BuyerProfileMemberId { get; set; }
	
	public long Start { get; set; }
	public long End { get; set; }
	
	public long SoldAt { get; set; }
	public long Price { get; set; }
	public short Count { get; set; }
	public bool Bin { get; set; }
	public Guid? ItemUuid { get; set; }
	
	[MaxLength(256)]
	public string? SkyblockId { get; set; }
	[MaxLength(512)]
	public string VariantKey { get; set; } = string.Empty;
	public required byte[] Item { get; set; }
	
	public DateTimeOffset LastUpdatedAt { get; set; }
	public long StartingBid { get; set; }
	public long? HighestBid { get; set; }
}

public class AuctionWorkItem
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }
	
	public required Guid AuctionId { get; set; }
	public required Auction Auction { get; set; }
}

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
	public void Configure(EntityTypeBuilder<Auction> builder) {
		builder.HasIndex(e => e.Price);
		builder.HasIndex(e => e.SoldAt);
		builder.HasIndex(e => new { e.SkyblockId, Timestamp = e.SoldAt });
		builder.HasIndex(e => new { e.SkyblockId, e.VariantKey, Timestamp = e.SoldAt });
		builder.HasIndex(e => e.SellerProfileMemberId);
		builder.HasIndex(e => e.BuyerProfileMemberId);
		builder.Property(e => e.VariantKey).HasDefaultValue(string.Empty);
		
		builder.HasOne<ProfileMember>()
			.WithMany(p => p.Auctions)
			.HasForeignKey(e => e.SellerProfileMemberId);
		
		builder.HasOne<ProfileMember>()
			.WithMany()
			.HasForeignKey(e => e.BuyerProfileMemberId);
	}
}