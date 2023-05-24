using EliteAPI.Data.Models.Hypixel;

namespace EliteAPI.Utilities;

public static class FormatUtils
{
    private static readonly int SkyblockEpochSeconds = 1560275700;
    public static DateTime GetTimeFromContestKey(string contestKey)
    {
        var split = contestKey.Split(":");
        if (split.Length != 3) return DateTime.MinValue;

        var year = int.Parse(split[0]);

        int[] monthDay = split[1].Split("_").Select(int.Parse).ToArray();
        if (monthDay.Length != 2) return DateTime.MinValue;

        var month = monthDay[0] - 1;
        var day = monthDay[1];

        return GetTimeFromSkyblockDate(year, month, day);
    }

    public static DateTime GetTimeFromSkyblockDate(int SkyblockYear, int SkyblockMonth, int SkyblockDay)
    {
        var days = SkyblockYear * 372 + SkyblockMonth * 31 + SkyblockDay;

        var seconds = days * 1200; // 1200 (60 * 20) seconds per day

        var unixTime = SkyblockEpochSeconds + seconds;

        return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
    }

    public static SkyblockDate GetSkyblockDate(DateTime dateTime) => new(dateTime);

    public static string GetReadableSkyblockDate(DateTime dateTime) => GetSkyblockDate(dateTime).ToString();

    public static Crop? GetCropFromContestKey(string contestKey)
    {
        var split = contestKey.Split(":");
        if (split.Length != 3) return null;

        return GetCropFromItemId(split[2]);
    }

    public static Crop? GetCropFromItemId(string itemId) => itemId switch
    {
        "CACTUS" => Crop.Cactus,
        "CARROT_ITEM" => Crop.Carrot,
        "INK_SACK:3" => Crop.CocoaBeans,
        "MELON" => Crop.Melon,
        "MUSHROOM_COLLECTION" => Crop.Mushroom,
        "NETHER_STALK" => Crop.NetherWart,
        "POTATO_ITEM" => Crop.Potato,
        "PUMPKIN" => Crop.Pumpkin,
        "SUGAR_CANE" => Crop.SugarCane,
        "WHEAT" => Crop.Wheat,
        _ => null
    };

    public static string GetSkyblockMonthName(int month) => month switch
    {
        0 => "Early Spring",
        1 => "Spring",
        2 => "Late Spring",
        3 => "Early Summer",
        4 => "Summer",
        5 => "Late Summer",
        6 => "Early Autumn",
        7 => "Autumn",
        8 => "Late Autumn",
        9 => "Early Winter",
        10 => "Winter",
        11 => "Late Winter",
        _ => "Invalid Month"
    };

    public static string AppendOrdinalSuffix(int number)
    {
        var j = number % 10;
        var k = number % 100;

        if (j == 1 && k != 11) return $"{number}st";
        if (j == 2 && k != 12) return $"{number}nd";
        if (j == 3 && k != 13) return $"{number}rd";

        return $"{number}th";
    }
}

public class SkyblockDate
{
    public static readonly int SkyblockEpochSeconds = 1560275700;
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public SkyblockDate(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    public SkyblockDate(DateTime dateTime)
    {
        var unixSeconds = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        var seconds = SkyblockEpochSeconds - unixSeconds;
        var days = seconds / 1200;

        var month = (int) Math.Floor(days % 372f / 31f);
        var day = (int) Math.Floor(days % 372f % 31f);

        Year = (int) Math.Floor(days / 372f);
        Month = day == 0 ? month - 1 : month;
        Day = day == 0 ? 31 : day;
    }

    public DateTime GetDateTime() => FormatUtils.GetTimeFromSkyblockDate(Year, Month, Day);
    public string MonthName() => FormatUtils.GetSkyblockMonthName(Month);
    public override string ToString()
    {
        return $"{MonthName()} {FormatUtils.AppendOrdinalSuffix(Day)}, Year {Year + 1}";
    }
}