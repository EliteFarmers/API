using EliteAPI.Features.HypixelGuilds.Services;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.HypixelGuilds.Commands;

public class UpdateGuildCommandHandler(
	IServiceScopeFactory scopeFactory
) : ICommandHandler<UpdateGuildCommand>
{
	public async Task ExecuteAsync(UpdateGuildCommand command, CancellationToken ct) {
		using var scope = scopeFactory.CreateScope();
		
		var mojangService = scope.ServiceProvider.GetRequiredService<IMojangService>();
		var guildService = scope.ServiceProvider.GetRequiredService<IHypixelGuildService>();
		
		var account = await mojangService.GetMinecraftAccountByUuid(command.PlayerUuid);
		if (account is null) return;
		
		await guildService.UpdateGuildIfNeeded(account, ct);
	}
}
