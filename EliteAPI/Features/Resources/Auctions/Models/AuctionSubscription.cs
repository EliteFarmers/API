using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Auctions.Models;

public class AuctionSubscription
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }
	
	public ulong AccountId { get; set; }
	public Guid ProfileMemberId { get; set; }

	
	public DateTimeOffset PausedUntil { get; set; }
}

public class AEndedAuctionConfiguration : IEntityTypeConfiguration<AuctionSubscription>
{
	public void Configure(EntityTypeBuilder<AuctionSubscription> builder) {
		builder.HasOne<ProfileMember>()
			.WithMany()
			.HasForeignKey(e => e.ProfileMemberId);
		
		builder.HasOne<EliteAccount>()
			.WithMany()
			.HasForeignKey(e => e.AccountId);
	}
}