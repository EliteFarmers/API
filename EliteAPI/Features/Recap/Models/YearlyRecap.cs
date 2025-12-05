using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Recap.Models;

public class YearlyRecap
{
	public Guid ProfileMemberId { get; set; }
	public ProfileMember ProfileMember { get; set; } = null!;
	public int Year { get; set; }

	public Guid Passkey { get; set; } = Guid.NewGuid();
	public bool Public { get; set; } = false;

	public int Views { get; set; } = 0;

	[Column(TypeName = "jsonb")] public YearlyRecapData Data { get; set; } = new();
}

public class YearlyRecapEntityConfiguration : IEntityTypeConfiguration<YearlyRecap>
{
	public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<YearlyRecap> builder) {
		builder.HasKey(x => new { x.ProfileMemberId, x.Year });
	}
}