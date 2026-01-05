using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;

namespace EliteAPI.Features.Profiles.Commands;

public class ProcessMemberDataCommand : ICommand
{
	public required string ProfileId { get; set; }
	public required string PlayerUuid { get; set; }
	public required string RequestedPlayerUuid { get; set; }
	public required ProfileMemberResponse MemberData { get; set; }
	public ProfileResponse? ProfileData { get; set; }
}
