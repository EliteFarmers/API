using EliteAPI.Features.Profiles.Services;
using FastEndpoints;

namespace EliteAPI.Features.Profiles.Commands;

public class RefreshPlayerDataCommandHandler(
	IServiceScopeFactory scopeFactory
) : ICommandHandler<RefreshPlayerDataCommand>
{
	public async Task ExecuteAsync(RefreshPlayerDataCommand command, CancellationToken ct) {
		using var scope = scopeFactory.CreateScope();
		
		var memberService = scope.ServiceProvider.GetRequiredService<IMemberService>();
		
		await memberService.RefreshPlayerData(command.PlayerUuid);
	}
}
