using FastEndpoints;

namespace EliteAPI.Features.HypixelGuilds.Commands;

public class UpdateGuildCommand : ICommand
{
	public required string PlayerUuid { get; set; }
}
