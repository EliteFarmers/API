using Prometheus;

namespace EliteAPI.Services;

public class MetricsService
{
    private static readonly Counter RequestCounter = Metrics.CreateCounter("api_requests_total", "Number of requests to the EliteAPI", new CounterConfiguration
    {
        LabelNames = new[] { "method", "endpoint", "status_code", "time_taken" }
    });

    public static void IncrementRequestCount(string method, string endpoint, string statusCode, string timeTaken)
    {
        RequestCounter.WithLabels(method, endpoint, statusCode, timeTaken).Inc();
    }
}
