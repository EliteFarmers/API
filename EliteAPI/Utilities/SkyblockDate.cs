namespace EliteAPI.Utilities;

public class SkyblockDate {
	public const int SkyblockEpochSeconds = 1560275700;
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public long UnixSeconds { get; private set; }
	public long ElapsedSeconds => UnixSeconds - SkyblockEpochSeconds;

	public static SkyblockDate Now => new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

	/// <summary>
	///     Creates a new SkyblockDate object from a Skyblock date, with numbers being 0 indexed.
	/// </summary>
	/// <param name="year"></param>
	/// <param name="month"></param>
	/// <param name="day"></param>
	public SkyblockDate(int year, int month, int day) {
		Year = year;
		Month = month;
		Day = day;

		UnixSeconds = FormatUtils.GetTimeFromSkyblockDate(year, month, day);
	}

	public SkyblockDate(long unixSeconds) {
		UnixSeconds = unixSeconds;
		var timeElapsed = unixSeconds - SkyblockEpochSeconds;
		var days = timeElapsed / 1200;

		var month = (int)Math.Floor(days % 372f / 31f);
		var day = (int)Math.Floor(days % 372f % 31f);

		Year = (int)Math.Floor(days / 372f);
		Month = month;
		Day = day;
	}

	public SkyblockDate(DateTime dateTime) : this(new DateTimeOffset(dateTime).ToUnixTimeSeconds()) {
	}

	public bool IsValid() {
		return Year >= 0 && Month >= 0 && Day >= 0;
	}

	public long StartOfDayTimestamp() {
		return FormatUtils.GetTimeFromSkyblockDate(Year, Month, Day);
	}

	public string MonthName() {
		return FormatUtils.GetSkyblockMonthName(Month);
	}

	public override string ToString() {
		return $"{MonthName()} {FormatUtils.AppendOrdinalSuffix(Day + 1)}, Year {Year + 1}";
	}
}