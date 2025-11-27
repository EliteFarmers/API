using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class DivanPowderCoatingHandler : IItemNetworthHandler
{
    public bool Applies(NetworthItem item)
    {
        return item.Attributes?.Extra != null && 
               item.Attributes.Extra.TryGetValue("divan_powder_coating", out var count) && 
               AttributeHelper.ToInt32(count) > 0;
    }

    public double Calculate(NetworthItem item, Dictionary<string, double> prices)
    {
        if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("divan_powder_coating", out var countObj))
        {
            return 0;
        }

        var count = AttributeHelper.ToInt32(countObj);
        
        if (prices.TryGetValue("DIVAN_POWDER_COATING", out var price))
        {
            var value = price * NetworthConstants.ApplicationWorth.DivanPowderCoating;

            item.Calculation ??= new List<NetworthCalculation>();
            item.Calculation.Add(new NetworthCalculation
            {
                Id = "DIVAN_POWDER_COATING",
                Type = "DIVAN_POWDER_COATING",
                Value = value,
                Count = count
            });

            return value;
        }

        return 0;
    }
}
