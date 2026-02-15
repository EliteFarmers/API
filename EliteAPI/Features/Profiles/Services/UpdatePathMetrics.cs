using System.Diagnostics.Metrics;
using FastEndpoints;

namespace EliteAPI.Features.Profiles.Services;

public interface IUpdatePathMetrics
{
	void RecordStage(string operation, string stage, double durationMs, bool success = true);
}

[RegisterService<IUpdatePathMetrics>(LifeTime.Singleton)]
public class UpdatePathMetrics : IUpdatePathMetrics
{
	private readonly Histogram<double> _duration;
	private readonly Counter<long> _calls;

	public UpdatePathMetrics(IMeterFactory meterFactory) {
		var meter = meterFactory.Create("eliteapi.update_path");
		_duration = meter.CreateHistogram<double>(
			"eliteapi.update_path.stage_duration_ms",
			unit: "ms",
			description: "Duration for update-path stages in milliseconds");
		_calls = meter.CreateCounter<long>(
			"eliteapi.update_path.stage_calls",
			description: "Number of update-path stage executions");
	}

	public void RecordStage(string operation, string stage, double durationMs, bool success = true) {
		var tags = new[] {
			new KeyValuePair<string, object?>("operation", operation),
			new KeyValuePair<string, object?>("stage", stage),
			new KeyValuePair<string, object?>("success", success ? "true" : "false")
		};

		_duration.Record(durationMs, tags);
		_calls.Add(1, tags);
	}
}
