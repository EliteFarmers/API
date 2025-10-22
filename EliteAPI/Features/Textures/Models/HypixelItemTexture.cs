using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Textures.Models;

public class HypixelItemTexture
{
	public string RenderHash { get; set; }
	
	public string Url { get; set; }
	
	public DateTimeOffset LastUsed { get; set; } = DateTimeOffset.UtcNow;
}

public class HypixelItemTextureEntityConfiguration :
	IEntityTypeConfiguration<HypixelItemTexture>
{
	public void Configure(EntityTypeBuilder<HypixelItemTexture> builder) {
		builder.HasKey(x => x.RenderHash);
	}
}