using FastEndpoints;

namespace EliteAPI.Features.Contests.UploadCurrentContests;

public class UploadCurrentContestsRequest {
	/// <summary>
	/// Upcoming contests
	/// </summary>
	[FromBody]
	public Dictionary<long, List<string>> Contests { get; set; }
}