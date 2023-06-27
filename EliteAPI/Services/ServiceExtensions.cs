using EliteAPI.Authentication;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using EliteAPI.Services.ProfileService;
using Microsoft.Extensions.Configuration.Json;
using Prometheus;
using StackExchange.Redis;

namespace EliteAPI.Services;

public static class ServiceExtensions
{

    public static void AddEliteServices(this IServiceCollection services) {
        // Add AutoMapper
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Add services to the container.
        services.AddSingleton<MetricsService>();

        services.AddHttpClient(HypixelService.HypixelService.HttpClientName, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EliteAPI");
        }).UseHttpClientMetrics();

        services.AddDbContext<DataContext>();
    }

    public static void AddEliteControllers(this IServiceCollection services)
    {
        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer(); 
        services.AddSwaggerGen();
    }

    public static void AddEliteScopedServices(this IServiceCollection services)
    {
        services.AddScoped<IHypixelService, HypixelService.HypixelService>();
        services.AddScoped<IMojangService, MojangService.MojangService>(); 
        services.AddScoped<IAccountService, AccountService.AccountService>();
        services.AddScoped<IProfileService, ProfileService.ProfileService>();
        services.AddScoped<IDiscordService, DiscordService.DiscordService>();

        services.AddScoped<ProfileParser>();
        services.AddScoped<DiscordAuthFilter>();
    }

    public static void AddEliteRedisCache(this IServiceCollection services)
    {
        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions()
        {
            EndPoints = { redisConnection },
            Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
            AbortOnConnectFail = false,
            ConnectRetry = 5
        });
        
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
    }

    public static void RegisterEliteConfigFiles(this WebApplicationBuilder builder)
    {
        builder.Configuration.Sources.Add(new JsonConfigurationSource()
        {
            Path = "Config\\Weight.json",
        });
        builder.Configuration.Sources.Add(new JsonConfigurationSource()
        {
            Path = "Config\\Cooldown.json",
        });

        builder.Services.Configure<ConfigFarmingWeightSettings>(builder.Configuration.GetSection("FarmingWeight"));
        builder.Services.Configure<ConfigCooldownSettings>(builder.Configuration.GetSection("CooldownSeconds"));
    }
}
