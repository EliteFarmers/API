using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Profiles.Models;

public class GameModeHistory
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }
	public string ProfileId { get; set; }
	public string Old { get; set; }
	public string New { get; set; }
	public DateTimeOffset ChangedAt { get; set; }
}

public class GameModeHistoryEntityConfiguration : IEntityTypeConfiguration<GameModeHistory>
{
	public void Configure(EntityTypeBuilder<GameModeHistory> builder) {
		builder.HasOne<Profile>()
			.WithMany()
			.HasForeignKey(x => x.ProfileId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}