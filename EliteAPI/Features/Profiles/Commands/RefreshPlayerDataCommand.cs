using FastEndpoints;

namespace EliteAPI.Features.Profiles.Commands;

public class RefreshPlayerDataCommand : ICommand
{
	public required string PlayerUuid { get; set; }
}
