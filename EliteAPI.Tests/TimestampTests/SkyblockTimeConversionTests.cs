using EliteAPI.Utilities;

namespace EliteAPI.Tests.TimestampTests; 

public class SkyblockTimeConversionTests {

    [Theory]
    [InlineData(1560275700, 0)]
    public void UnixSecondsToSkyblockTimeTest(long unixSeconds, long expectedSkyblockTime) {
        var actual = new SkyblockDate(unixSeconds).ElapsedSeconds;
        
        actual.ShouldBe(expectedSkyblockTime);
    }
    
    [Theory]
    [InlineData(0, 0, 0, 1560275700)]
    [InlineData(0, 0, 1, 1560275700 + 60 * 20)]
    [InlineData(0, 0, 5, 1560275700 + 60 * 20 * 5)]
    [InlineData(0, 0, 30, 1560275700 + 60 * 20 * 30)]
    [InlineData(0, 0, 31, 1560275700 + 60 * 20 * 31)]
    [InlineData(0, 1, 0, 1560275700 + 60 * 20 * 31)]
    public void SkyblockDateToUnixSecondsTest(int year, int month, int day, long unixSeconds) {
        var expected = new SkyblockDate(year, month, day).UnixSeconds;
        
        unixSeconds.ShouldBe(expected);
    }
    
    [Theory]
    [InlineData(1560275700, 0, 0, 0)]
    [InlineData(1560275700 + 60 * 20, 0, 0, 1)]
    [InlineData(1560275700 + 60 * 20 * 5, 0, 0, 5)]
    [InlineData(1560275700 + 60 * 20 * 30, 0, 0, 30)]
    [InlineData(1560275700 + 60 * 20 * 31, 0, 1, 0)]
    public void UnixSecondsToSkyblockDateTest(long unixSeconds, int year, int month, int day) {
        var date = new SkyblockDate(unixSeconds);
        
        date.Day.ShouldBe(day);
        date.Month.ShouldBe(month);
        date.Year.ShouldBe(year);
    }
    
    [Theory]
    [InlineData(1687904100, "Winter 28th, Year 286")]
    [InlineData(1605809700, "Early Spring 2nd, Year 103")]
    [InlineData(1606252500, "Late Winter 30th, Year 103")]
    public void UnixSecondsToReadableSkyblockDateTest(long timestamp, string formattedDate) {
        var actual = new SkyblockDate(timestamp).ToString();
        
        actual.ShouldBe(formattedDate);
    }
    
    [Theory]
    [InlineData(1560275700, 1560275700)]
    [InlineData(1560275705 + 60 * 20, 1560275700 + 60 * 20)]
    [InlineData(1560275740 + 60 * 20 * 5, 1560275700 + 60 * 20 * 5)]
    [InlineData(1560275705 + 60 * 20 * 30, 1560275700 + 60 * 20 * 30)]
    [InlineData(1560275701 + 60 * 20 * 31, 1560275700 + 60 * 20 * 31)]
    public void UnixSecondsToStartOfSkyblockDateTest(long unixSeconds, long expected) {
        var date = new SkyblockDate(unixSeconds);
        
        date.StartOfDayTimestamp().ShouldBe(expected);
    }
}