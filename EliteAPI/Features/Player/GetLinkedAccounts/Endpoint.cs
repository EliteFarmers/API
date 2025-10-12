using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Player.GetLinkedAccounts;

using Result = Results<Ok<LinkedAccountsDto>, NotFound>;

internal sealed class GetLinkedAccountsEndpoint(
	DataContext context,
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, Result> {
	public override void Configure() {
		Get("/player/{DiscordId}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Linked Accounts"; });
	}

	public override async Task<Result> ExecuteAsync(DiscordIdRequest request, CancellationToken c) {
		var account = await context.Accounts
			.Include(a => a.MinecraftAccounts)
			.FirstOrDefaultAsync(a => a.Id == request.DiscordIdUlong, c);

		if (account is null) return TypedResults.NotFound();

		var minecraftAccounts = account.MinecraftAccounts;
		var dto = new LinkedAccountsDto();

		foreach (var minecraftAccount in minecraftAccounts) {
			var data = await profileService.GetPlayerData(minecraftAccount.Id);
			if (data is null) continue;

			if (minecraftAccount.Selected) dto.SelectedUuid = data.Uuid;

			dto.Players.Add(mapper.Map<PlayerDataDto>(data));
		}

		return TypedResults.Ok(dto);
	}
}