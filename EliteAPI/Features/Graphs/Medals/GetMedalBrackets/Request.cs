using System.ComponentModel;
using EliteAPI.Features.Contests;
using FastEndpoints;

namespace EliteAPI.Features.Graphs.Medals.GetMedalBrackets;

internal sealed class GetMedalBracketsRequest : SkyBlockMonthRequest
{
	/// <summary>
	/// Amount of previous SkyBlock months to include in the average
	/// </summary>
	[QueryParam]
	[DefaultValue(2)]
	public int? Months { get; set; } = 2;
}

internal sealed class GetMedalBracketsRequestValidator : Validator<GetMedalBracketsRequest>
{
	public GetMedalBracketsRequestValidator() {
		Include(new SkyBlockMonthRequestValidator());
	}
}