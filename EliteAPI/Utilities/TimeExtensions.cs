namespace EliteAPI.Utilities;

public static class TimeExtensions
{
    public static bool IsValidJacobContestTime(this long unixTimeSeconds, int fromYear = -1)
    {
        var correctMinute = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).Minute == 15;
        if (fromYear == -1) return correctMinute;
        
        return correctMinute && new SkyblockDate(unixTimeSeconds).Year == fromYear;
    }
    
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
