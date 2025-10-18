using System.ComponentModel;
using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetEvents;

public class SetEventFeatureRequest : DiscordIdRequest
{
	[QueryParam] [DefaultValue(false)] public bool? Enable { get; set; } = false;

	[QueryParam] [DefaultValue(false)] public int? Max { get; set; } = 1;
}

internal sealed class SetEventFeatureRequestValidator : Validator<SetEventFeatureRequest>
{
	public SetEventFeatureRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}