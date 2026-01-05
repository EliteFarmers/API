using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Services.Commands;

public class RefreshMinecraftAccountCommandHandler(
	IServiceScopeFactory scopeFactory
) : ICommandHandler<RefreshMinecraftAccountCommand>
{
	public async Task ExecuteAsync(RefreshMinecraftAccountCommand command, CancellationToken ct) {
		using var scope = scopeFactory.CreateScope();
		
		var mojangService = scope.ServiceProvider.GetRequiredService<IMojangService>();
		
		await mojangService.RefreshMinecraftAccount(command.Uuid);
	}
}
