using EliteAPI.Utilities;

namespace EliteAPI.Tests.TimestampTests; 

public class ContestTimestampTests {
    
    [Theory]
    [InlineData("285:11_28:PUMPKIN", 1687904100)]
    public void ContestKeyTimestampTest(string contestKey, long expectedTimestamp) {
        var actual = FormatUtils.GetTimeFromContestKey(contestKey);
        
        actual.Should().Be(expectedTimestamp);
    }
    
    [Theory]
    [InlineData(1687904100, "Late Winter 28th, Year 286")]
    public void ContestKeyFromTimestampTest(long timestamp, string formattedDate) {
        var actual = new SkyblockDate(timestamp).ToString();
        
        actual.Should().Be(formattedDate);
    }
}