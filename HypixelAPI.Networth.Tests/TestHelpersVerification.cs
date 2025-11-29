using HypixelAPI.Networth.Tests.Infrastructure;
using Xunit;

namespace HypixelAPI.Networth.Tests;

public class TestHelpersVerification
{
    [Fact]
    public void VerifyBasePriceDeserialization()
    {
        var itemData = new { BasePrice = 50000 };
        var item = TestHelpers.CreateItem(itemData);
        Assert.Equal(50000, item.BasePrice);
    }
}
