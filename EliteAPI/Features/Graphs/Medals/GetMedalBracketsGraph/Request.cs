using System.ComponentModel;
using EliteAPI.Features.Contests;
using FastEndpoints;

namespace EliteAPI.Features.Graphs.Medals.GetMedalBracketsGraph;

internal sealed class GetMedalBracketsGraphRequest : SkyBlockYearRequest
{
	/// <summary>
	/// Amount of previous SkyBlock years to include in the average
	/// </summary>
	[QueryParam]
	[DefaultValue(2)]
	public int? Years { get; set; } = 2;

	/// <summary>
	/// Amount of previous SkyBlock months to include in the average
	/// </summary>
	[QueryParam]
	[DefaultValue(2)]
	public int? Months { get; set; } = 2;
}

internal sealed class GetMedalBracketsGraphRequestValidator : Validator<GetMedalBracketsGraphRequest>
{
	public GetMedalBracketsGraphRequestValidator() {
		Include(new SkyBlockYearRequestValidator());
	}
}