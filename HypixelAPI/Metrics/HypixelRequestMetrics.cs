using System.Diagnostics.Metrics;

namespace EliteFarmers.HypixelAPI.Metrics;

public interface IHypixelRequestMetrics
{
	void RecordRequest(string endpoint, string method, int? statusCode, double durationMs, string result);
}

public class HypixelRequestMetrics : IHypixelRequestMetrics
{
	private readonly Histogram<double> _requestDuration;
	private readonly Counter<long> _requests;

	public HypixelRequestMetrics(IMeterFactory meterFactory) {
		var meter = meterFactory.Create("hypixel.api");
		_requestDuration = meter.CreateHistogram<double>(
			"hypixel.api.request_duration_ms",
			unit: "ms",
			description: "Duration of Hypixel API client requests in milliseconds.");
		_requests = meter.CreateCounter<long>(
			"hypixel.api.requests",
			description: "Number of Hypixel API client requests.");
	}

	public void RecordRequest(string endpoint, string method, int? statusCode, double durationMs, string result) {
		var tags = new[] {
			new KeyValuePair<string, object?>("endpoint", endpoint),
			new KeyValuePair<string, object?>("method", method),
			new KeyValuePair<string, object?>("status_code", statusCode?.ToString() ?? "none"),
			new KeyValuePair<string, object?>("result", result)
		};

		_requestDuration.Record(durationMs, tags);
		_requests.Add(1, tags);
	}
}
