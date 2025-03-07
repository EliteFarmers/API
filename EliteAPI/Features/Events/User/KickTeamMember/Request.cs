using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Events.User.KickTeamMember;

internal sealed class KickTeamMemberRequest : PlayerRequest {
	[BindFrom("eventId")]
	public ulong EventId { get; set; }
	[BindFrom("teamId")]
	public int TeamId { get; set; }
}

internal sealed class KickTeamMemberRequestValidator : Validator<KickTeamMemberRequest> {
	public KickTeamMemberRequestValidator() {
		Include(new PlayerRequestValidator());
	}
}
