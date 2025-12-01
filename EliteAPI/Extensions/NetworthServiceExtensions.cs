using HypixelAPI.Networth.Calculators;
using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Interfaces;

namespace EliteAPI.Extensions;

public static class NetworthServiceExtensions
{
    public static IServiceCollection AddNetworthServices(this IServiceCollection services)
    {
        // Register the calculator
        services.AddScoped<SkyBlockItemNetworthCalculator>();
        services.AddScoped<PetNetworthCalculator>();

        // Register all handlers
        var assembly = typeof(IItemNetworthHandler).Assembly;
        var handlerTypes = assembly.GetTypes()
            .Where(t => typeof(IItemNetworthHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(typeof(IItemNetworthHandler), handlerType);
        }

        return services;
    }
}
