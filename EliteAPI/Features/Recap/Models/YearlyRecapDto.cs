using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Recap.Models;

[Mapper]
public static partial class YearlyRecapDtoMapper
{
	public static YearlyRecapDto FromYearlyRecap(YearlyRecap recap, GlobalRecap? globalRecap) {
		var dto = new YearlyRecapDto() {
			PlayerUuid = recap.ProfileMember.PlayerUuid,
			Year = recap.Year,
			Data = recap.Data,
			Public = recap.Public,
			Global = globalRecap ?? new GlobalRecap()
		};

		if (recap.ProfileMember.MinecraftAccount?.EliteAccount != null) {
			dto.Discord = new DiscordRecapInfoDto {
				Username = recap.ProfileMember.MinecraftAccount.EliteAccount.Username ?? "",
				Avatar = recap.ProfileMember.MinecraftAccount.EliteAccount.Avatar ?? "",
				Id = recap.ProfileMember.MinecraftAccount.AccountId?.ToString() ?? ""
			};
		}

		return dto;
	}
}

public class YearlyRecapDto
{
	public string PlayerUuid { get; set; } = string.Empty;
	public int Year { get; set; }
	public bool Public { get; set; }
	public YearlyRecapData Data { get; set; } = new();
	public GlobalRecap Global { get; set; } = new();
	public DiscordRecapInfoDto Discord { get; set; } = new();
}

public class DiscordRecapInfoDto
{
	public string Username { get; set; } = string.Empty;
	public string Id { get; set; } = string.Empty;
	public string Avatar { get; set; } = string.Empty;
}