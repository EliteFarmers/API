using FastEndpoints;

namespace EliteAPI.Services.Commands;

public class RefreshMinecraftAccountCommand : ICommand
{
	public required string Uuid { get; set; }
}
