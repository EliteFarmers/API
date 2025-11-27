using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public interface IItemNetworthHandler
{
	bool Applies(NetworthItem item);
	double Calculate(NetworthItem item, Dictionary<string, double> prices);
}