using EliteAPI.Data;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.SearchAccounts;

internal sealed class AccountSearchResultDto
{
	public required string Ign { get; set; }
	public required string Uuid { get; set; }
	public string? DiscordId { get; set; }
}

internal sealed class SearchAccountsWithDiscordEndpoint(
	DataContext context
) : EndpointWithoutRequest<List<AccountSearchResultDto>>
{
	public override void Configure() {
		Get("/account/discord-search");
		Version(0);

		Summary(s => { s.Summary = "Search for Minecraft Account From Discord";
			s.Description =
				"Authenticated endpoint that returns a list of accounts that have a specific discord username linked.";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var name = User.GetDiscordUsername();
		if (name is null) {
			await Send.UnauthorizedAsync(c);
			return;
		}

		var result = await context.PlayerData
			.Where(p => p.SocialMedia.Discord == name)
			.Include(p => p.MinecraftAccount)
			.Select(p => new AccountSearchResultDto {
				Ign = p.MinecraftAccount!.Name,
				Uuid = p.MinecraftAccount.Id,
				DiscordId = p.MinecraftAccount.AccountId == null ? null : p.MinecraftAccount.AccountId.ToString()
			}).ToListAsync(c);

		await Send.OkAsync(result, c);
	}
}