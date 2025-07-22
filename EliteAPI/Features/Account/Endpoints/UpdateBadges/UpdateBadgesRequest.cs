using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Incoming;
using FastEndpoints;

namespace EliteAPI.Features.Account.UpdateBadges;

public class UpdateBadgesRequest : PlayerUuidRequest {
	[FromBody] public List<EditUserBadgeDto> Badges { get; set; } = [];
}

internal sealed class UpdateBadgesRequestValidator : Validator<UpdateBadgesRequest> {
	public UpdateBadgesRequestValidator() {
		Include(new PlayerUuidRequestValidator());	
	}
}