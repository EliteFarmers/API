using System.ComponentModel;
using FastEndpoints;

namespace EliteAPI.Features.Contests.GetContestsInYear;

internal sealed class GetContestsInYearRequest : SkyBlockDayRequest {
	/// <summary>
	/// If the year being requested is the current year. Not required.
	/// </summary>
	[QueryParam, DefaultValue(false)]
	public bool? Now { get; set; }
}
