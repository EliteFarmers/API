using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Announcements.Models;

public class DismissedAnnouncement {
	[ForeignKey(nameof(Announcement))] public Guid AnnouncementId { get; set; }
	public Announcement Announcement { get; set; } = null!;

	[ForeignKey(nameof(EliteAccount))] public ulong AccountId { get; set; }
	public EliteAccount EliteAccount { get; set; } = null!;
}

public class DismissedAnnouncementConfiguration : IEntityTypeConfiguration<DismissedAnnouncement> {
	public void Configure(EntityTypeBuilder<DismissedAnnouncement> builder) {
		builder.HasKey(da => new { da.AnnouncementId, da.AccountId });
	}
}