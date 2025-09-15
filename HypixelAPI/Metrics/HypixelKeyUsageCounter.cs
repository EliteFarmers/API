using System.Diagnostics.Metrics;

namespace EliteFarmers.HypixelAPI.Metrics;

public interface IHypixelKeyUsageCounter {
	void Increment(int value = 1);
}

public class HypixelKeyUsageCounter : IHypixelKeyUsageCounter {
	private readonly Counter<int> _keyUsageCounter;

	public HypixelKeyUsageCounter(IMeterFactory meterFactory) {
		var meter = meterFactory.Create("hypixel.api");
		_keyUsageCounter = meter.CreateCounter<int>("hypixel.api.key_usage", description: "The number of requests used.");
	}
	
	public void Increment(int value = 1) {
		_keyUsageCounter.Add(value);
	}
}