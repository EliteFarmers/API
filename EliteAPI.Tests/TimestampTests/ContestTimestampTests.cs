using EliteAPI.Utilities;

namespace EliteAPI.Tests.TimestampTests; 

public class ContestTimestampTests {
    
    [Theory]
    [InlineData("285:11_28:PUMPKIN", 1687904100)]
    public void ContestKeyTimestampTest(string contestKey, long expectedTimestamp) {
        var actual = FormatUtils.GetTimeFromContestKey(contestKey);
        
        actual.Should().Be(expectedTimestamp);
    }
}