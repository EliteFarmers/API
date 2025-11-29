namespace HypixelAPI.Networth.Models;

public class NetworthCalculationData
{
    public double Value { get; set; }
    public double SoulboundValue { get; set; }
    public bool IsCosmetic { get; set; }
    
    public static implicit operator NetworthCalculationData(double value)
    {
        return new NetworthCalculationData { Value = value };
    }
}