using System.ComponentModel;
using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetJacobLeaderboards;

public class SetJacobFeatureRequest : DiscordIdRequest {
	[QueryParam] [DefaultValue(true)] public bool? Enable { get; set; } = true;

	[QueryParam] [DefaultValue(false)] public int? Max { get; set; } = 1;
}

internal sealed class SetJacobFeatureRequestValidator : Validator<SetJacobFeatureRequest> {
	public SetJacobFeatureRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}