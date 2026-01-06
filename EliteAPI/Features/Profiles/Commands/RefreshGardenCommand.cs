using FastEndpoints;

namespace EliteAPI.Features.Profiles.Commands;

public class RefreshGardenCommand : ICommand
{
	public required string ProfileId { get; set; }
	public required long ProfileResponseHash { get; set; }
}
