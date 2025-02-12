using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Events.Admin.GetGuildEvent;

internal sealed class GetAdminGuildEventRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetAdminGuildEventRequestValidator : Validator<GetAdminGuildEventRequest> {
	public GetAdminGuildEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}