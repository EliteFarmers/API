using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Account.Models;

[Mapper]
public static partial class AccountMetaMapper
{
	public static partial IQueryable<AccountMetaDto> SelectMetaDto(this IQueryable<MinecraftAccount> query); 
	
	public static AccountMetaDto ToMetaDto(MinecraftAccount account) {
		var prefix = account.EliteAccount?.UserSettings.Prefix ?? string.Empty;
		var suffix = account.EliteAccount?.UserSettings.Suffix ?? string.Empty;
		
		return new AccountMetaDto() {
			Id = account.Id,
			Name = account.Name,
			FormattedName = $"{prefix} {account.Name} {suffix}".Trim()
		};
	}
}

public class AccountMetaDto
{
	public required string Id { get; set; }
	public required string Name { get; set; }
	public required string FormattedName { get; set; }
}