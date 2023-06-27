namespace EliteAPI.Utilities;

public static class TimeExtensions
{
    public static bool OlderThanSeconds(this long unixTimeSeconds, int seconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime < DateTime.UtcNow.AddSeconds(-seconds);
    }

    public static bool OlderThanMinutes(this long unixTimeSeconds, int minutes)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime < DateTime.UtcNow.AddMinutes(-minutes);
    }

    public static bool OlderThanHours(this long unixTimeSeconds, int hours)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime < DateTime.UtcNow.AddHours(-hours);
    }

    public static bool OlderThanDays(this long unixTimeSeconds, int days)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime < DateTime.UtcNow.AddDays(-days);
    }

    public static string ToReadableSkyblockDate(this long unixTimeSeconds)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);

        return FormatUtils.GetReadableSkyblockDate(dateTimeOffset.UtcDateTime);
    }
}
