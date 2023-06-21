using EliteAPI.Parsers.Profiles;
using FluentAssertions;

namespace EliteAPI.Tests.ParserTests;

public class CollectionTierParserTests
{
    [Fact]
    public void CollectionTierParserTest()
    {
        var input = new[] { "WHEAT_1", "SUGAR_CANE_1", "WHEAT_5", "SUGAR_CANE_2", "POTATO_ITEM_3" };
        var expected = new Dictionary<string, int>
        {
            { "WHEAT", 5 },
            { "SUGAR_CANE", 2 },
            { "POTATO_ITEM", 3 }
        };

        var actual = CollectionTierParser.Parse(input);

        actual.Should().Equal(expected);
    }
}
