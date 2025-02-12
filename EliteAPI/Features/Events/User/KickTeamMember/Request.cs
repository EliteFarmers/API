using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Events.User.KickTeamMember;

internal sealed class KickTeamMemberRequest : PlayerRequest {
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
}

internal sealed class KickTeamMemberRequestValidator : Validator<KickTeamMemberRequest> {
	public KickTeamMemberRequestValidator() {
		Include(new PlayerRequestValidator());
	}
}
