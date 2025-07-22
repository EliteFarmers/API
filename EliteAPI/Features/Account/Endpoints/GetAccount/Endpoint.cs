using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Account.GetAccount;

internal sealed class GetAccountEndpoint(
	IMemberService memberService,
	IAccountService accountService,
	IMojangService mojangService,
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<PlayerRequest, MinecraftAccountDto> {
	
	public override void Configure() {
		Get("/account/{Player}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Minecraft Account";
		});
	}

	public override async Task HandleAsync(PlayerRequest request, CancellationToken c) {
		await memberService.UpdatePlayerIfNeeded(request.Player);
        
		var account = await accountService.GetAccountByIgnOrUuid(request.Player);
        
		var minecraftAccount = account is null
			? await mojangService.GetMinecraftAccountByUuidOrIgn(request.Player)
			: account.MinecraftAccounts.Find(m => m.Id.Equals(request.Player) || m.Name.Equals(request.Player)) 
			  ?? account.MinecraftAccounts.Find(m => m.Selected) ?? account.MinecraftAccounts.FirstOrDefault();

		if (minecraftAccount is null) {
			ThrowError("Minecraft account not found", StatusCodes.Status404NotFound);
		}
        
		var profilesDetails = await profileService.GetProfilesDetails(minecraftAccount.Id);
		var playerData = await profileService.GetPlayerData(minecraftAccount.Id);
        
		var mappedPlayerData = mapper.Map<PlayerDataDto>(playerData);
		var result = mapper.Map<MinecraftAccountDto>(minecraftAccount);

		result.DiscordId = account?.Id.ToString();
		result.DiscordUsername = account?.Username;
		result.DiscordAvatar = account?.Avatar;
		result.PlayerData = mappedPlayerData;
		result.Profiles = profilesDetails;
		result.Settings = mapper.Map<UserSettingsDto>(account?.UserSettings);

		await SendAsync(result, cancellation: c);
	}
}