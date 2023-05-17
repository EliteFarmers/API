using Prometheus;

namespace EliteAPI.Services;

public class MetricsService
{
    private static readonly Counter RequestCounter = Metrics.CreateCounter("elite_api_requests_total", "Number of requests to the EliteAPI", new CounterConfiguration
    {
        LabelNames = new[] { "method", "endpoint", "status_code", "time_taken" }
    });

    private static readonly Counter ProfilesTransformedCounter = Metrics.CreateCounter("elite_profiles_transformed_total", "Number of profiles transformed", new CounterConfiguration
    {
        LabelNames = new[] { "profile_id" }
    });

    public static void IncrementRequestCount(string method, string endpoint, string statusCode, string timeTaken)
    {
        RequestCounter.WithLabels(method, endpoint, statusCode, timeTaken).Inc();
    }

    public static void IncrementProfilesTransformedCount(string profileId)
    {
        ProfilesTransformedCounter.WithLabels(profileId).Inc();
    }
}
