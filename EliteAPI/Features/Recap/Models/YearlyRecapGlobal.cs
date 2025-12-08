using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Recap.Models;

public class YearlyRecapSnapshot
{
	public int Year { get; set; }
	
	[System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
	public GlobalRecap Data { get; set; } = new();
}

public class YearlyRecapGlobalEntityConfiguration : IEntityTypeConfiguration<YearlyRecapSnapshot>
{
	public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<YearlyRecapSnapshot> builder) {
		builder.HasKey(x => x.Year);
	}
}