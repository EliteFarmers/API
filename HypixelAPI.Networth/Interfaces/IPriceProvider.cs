namespace HypixelAPI.Networth.Interfaces;

public interface IPriceProvider
{
    Task<Dictionary<string, double>> GetPricesAsync();
}
