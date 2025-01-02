using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Monetization;

public class Tag {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
	public int Id { get; set; }

	[MaxLength(256)]
	public required string Title { get; set; }
	
	[MaxLength(32)]
	public required string Slug { get; set; }
	
	[MaxLength(512)]
	public string? Description { get; set; }
	
	public int Order { get; set; }
	
	public List<ProductTag> ProductTags { get; set; } = [];
	public List<Product> Products { get; set; } = [];
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
	public void Configure(EntityTypeBuilder<Tag> builder)
	{
		builder
			.HasMany(e => e.Products)
			.WithMany(e => e.Tags)
			.UsingEntity<ProductCategory>();

		builder.HasIndex(p => p.Slug).IsUnique();
	}
}
