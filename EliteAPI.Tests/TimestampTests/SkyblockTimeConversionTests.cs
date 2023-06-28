using EliteAPI.Utilities;

namespace EliteAPI.Tests.ContestTests; 

public class SkyblockTimeConversionTests {

    [Theory]
    [InlineData(1560275700, 0)]
    public void UnixSecondsToSkyblockTimeTest(long unixSeconds, long expectedSkyblockTime) {
        var actual = new SkyblockDate(unixSeconds).ElapsedSeconds;
        
        actual.Should().Be(expectedSkyblockTime);
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
        
        unixSeconds.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(1560275700, 0, 0, 0)]
    [InlineData(1560275700 + 60 * 20, 0, 0, 1)]
    [InlineData(1560275700 + 60 * 20 * 5, 0, 0, 5)]
    [InlineData(1560275700 + 60 * 20 * 30, 0, 0, 30)]
    [InlineData(1560275700 + 60 * 20 * 31, 0, 1, 0)]
    public void UnixSecondsToSkyblockDateTest(long unixSeconds, int year, int month, int day) {
        var date = new SkyblockDate(unixSeconds);
        
        date.Day.Should().Be(day);
        date.Month.Should().Be(month);
        date.Year.Should().Be(year);
    }
}