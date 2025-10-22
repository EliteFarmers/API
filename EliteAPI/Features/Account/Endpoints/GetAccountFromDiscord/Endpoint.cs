using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Account.GetAccountFromDiscord;

internal sealed class GetAccountFromDiscordEndpoint(
	IAccountService accountService,
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, MinecraftAccountDto>
{
	public override void Configure() {
		Get("/account/{DiscordId:long:minlength(17)}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Minecraft Account from Discord Id"; });

		ResponseCache(120);
		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2))); });
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var account = await accountService.GetAccount(request.DiscordIdUlong)
		              ?? await accountService.GetAccountByIgnOrUuid(request.DiscordId.ToString());

		if (account is null) ThrowError("Minecraft account not found", StatusCodes.Status404NotFound);

		var minecraftAccount = account.MinecraftAccounts.Find(m => m.Selected) ??
		                       account.MinecraftAccounts.FirstOrDefault();
		if (minecraftAccount is null) ThrowError("Linked account not found", StatusCodes.Status404NotFound);

		var profileDetails = await profileService.GetProfilesDetails(minecraftAccount.Id);

		var playerData = await profileService.GetPlayerData(minecraftAccount.Id);
		var result = mapper.Map<MinecraftAccountDto>(minecraftAccount);

		result.DiscordId = account.Id.ToString();
		result.DiscordUsername = account.Username;
		result.DiscordAvatar = account.Avatar;
		result.PlayerData = mapper.Map<PlayerDataDto>(playerData);
		result.Profiles = profileDetails;
		result.Settings = mapper.Map<UserSettingsDto>(account.UserSettings);

		await Send.OkAsync(result, c);
	}
}