using System.ComponentModel;
using EliteAPI.Models.Common;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetPublic;

public class SetGuildPublicRequest : DiscordIdRequest
{
	[QueryParam] [DefaultValue(true)] public bool? Public { get; set; } = true;
}

internal sealed class SetGuildPublicRequestValidator : Validator<SetGuildPublicRequest>
{
	public SetGuildPublicRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}