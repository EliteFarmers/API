using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace EliteAPI.Data;

public class Instrumentation : IDisposable
{
    private const string ActivitySourceName = "Elite.API";
    private const string MeterName = "Elite.API";
    private readonly Meter meter;

    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();
        ActivitySource = new ActivitySource(ActivitySourceName, version);
        meter = new Meter(MeterName, version);

        FreezingDaysCounter = meter.CreateCounter<long>("weather.days.freezing", "The number of days where the temperature is below freezing");
    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> FreezingDaysCounter { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ActivitySource.Dispose();
        meter.Dispose();
    }
}