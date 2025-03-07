using System.ComponentModel;
using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Admin.Events.SetEventApproval;

public class SetEventApprovalRequest : EventIdRequest {
	[QueryParam, DefaultValue(false)]
	public bool? Approve { get; set; } = false;
}

internal sealed class SetEventApprovalRequestValidator : Validator<SetEventApprovalRequest> {
	public SetEventApprovalRequestValidator() {
		Include(new EventIdRequestValidator());
	}
}