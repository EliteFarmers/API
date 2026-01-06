using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
	public void Configure(EntityTypeBuilder<IdentityRole> builder) {
		builder.HasData(
			new IdentityRole {
				Id = "8270a1b1-5809-436a-ba1c-b712f4f55f67",
				Name = "Admin",
				NormalizedName = "ADMIN",
				ConcurrencyStamp = "11d759b6-025f-4334-b8fc-3b26a72cda87"
			},
			new IdentityRole {
				Id = "3384aba1-5453-4787-81d9-0b7222225d81",
				Name = "Moderator",
				NormalizedName = "MODERATOR",
				ConcurrencyStamp = "34eb8585-7920-4f4c-857a-e5d131a835ef"
			},
			new IdentityRole {
				Id = "d8c803c1-63a0-4594-8d68-aad7bd59df7d",
				Name = "Support",
				NormalizedName = "SUPPORT",
				ConcurrencyStamp = "e0e6b08b-7cd1-4f8c-b827-cab959ebc9be"
			},
			new IdentityRole {
				Id = "ff4f5319-644e-4332-8bd5-2ec989ba5e7f",
				Name = "Wiki",
				NormalizedName = "WIKI",
				ConcurrencyStamp = "e4ec974b-71af-4307-8bf0-3feb9f380566"
			},
			new IdentityRole {
				Id = "e99efab5-3fd2-416e-b8f5-93b0370892ac",
				Name = "User",
				NormalizedName = "USER",
				ConcurrencyStamp = "c9ad7f78-129a-4507-ace1-5a71c221a901"
			}
		);
	}
}