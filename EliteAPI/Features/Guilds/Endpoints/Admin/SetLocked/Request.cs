using System.ComponentModel;
using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetLocked;

public class SetGuildLockedRequest : DiscordIdRequest {
	/// <summary>
	/// If server subscriptions shouldn't override feature values
	/// </summary>
	[QueryParam]
	[DefaultValue(true)]
	public bool? Locked { get; set; } = true;
}

internal sealed class SetGuildLockedRequestValidator : Validator<SetGuildLockedRequest> {
	public SetGuildLockedRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}