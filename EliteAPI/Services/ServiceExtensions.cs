using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using EliteAPI.Authentication;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Parsers.Skyblock;
using EliteAPI.RateLimiting;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.CacheService;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.GuildService;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.LeaderboardService;
using EliteAPI.Services.MemberService;
using EliteAPI.Services.MojangService;
using EliteAPI.Services.ProfileService;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.OpenApi.Models;
using Prometheus;
using StackExchange.Redis;

namespace EliteAPI.Services;

public static class ServiceExtensions
{
    public static void AddEliteServices(this IServiceCollection services) {
        // Add AutoMapper
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Add services to the container.
        // services.AddSingleton<MetricsService>();
        services.AddSingleton<HypixelRequestLimiter>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

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
        services.AddSwaggerGen(opt => {
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme 
            {
                In = ParameterLocation.Header,
                Description = "Enter Discord Bearer Token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            
            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference 
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    new string[] {}
                }
            });
            
            opt.SwaggerDoc("v1", new OpenApiInfo {
                Version = "v1",
                Title = "EliteAPI",
                Description = "A backend API for https://elitebot.dev/ that provides Hypixel Skyblock data. Use of this API requires following the TOS linked below. This API is not affiliated with Hypixel or Mojang.",
                Contact = new OpenApiContact
                {
                    Name = "GitHub",
                    Url = new Uri("https://github.com/EliteFarmers/API")
                },
                License = new OpenApiLicense
                {
                    Name = "GPL-3.0",
                    Url = new Uri("https://github.com/EliteFarmers/API/blob/master/LICENSE.txt")
                },
                TermsOfService = new Uri("https://elitebot.dev/apiterms")
            });

            opt.SupportNonNullableReferenceTypes();
            opt.EnableAnnotations();
        });
    }

    public static void AddEliteScopedServices(this IServiceCollection services) {
        services.AddScoped<ICacheService, CacheService.CacheService>();
        services.AddScoped<IHypixelService, HypixelService.HypixelService>();
        services.AddScoped<IMojangService, MojangService.MojangService>();
        services.AddScoped<IMemberService, MemberService.MemberService>();
        services.AddScoped<IAccountService, AccountService.AccountService>();
        services.AddScoped<IProfileService, ProfileService.ProfileService>();
        services.AddScoped<IDiscordService, DiscordService.DiscordService>();
        services.AddScoped<ILeaderboardService, LeaderboardService.LeaderboardService>();
        services.AddScoped<IGuildService, GuildService.GuildService>();

        services.AddScoped<ProfileParser>();
        services.AddScoped<DiscordAuthFilter>();
        services.AddScoped<DiscordBotOnlyFilter>();
    }

    public static void AddEliteRedisCache(this IServiceCollection services)
    {
        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
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
            Path = "Config/Weight.json",
        });
        builder.Configuration.Sources.Add(new JsonConfigurationSource()
        {
            Path = "Config/Cooldown.json",
        });
        builder.Configuration.Sources.Add(new JsonConfigurationSource()
        {
            Path = "Config/Leaderboards.json",
        });
        builder.Configuration.Sources.Add(new JsonConfigurationSource()
        {
            Path = "Config/FarmingItems.json",
        });
        
        builder.Services.Configure<ConfigFarmingWeightSettings>(builder.Configuration.GetSection("FarmingWeight"));
        builder.Services.Configure<ConfigCooldownSettings>(builder.Configuration.GetSection("CooldownSeconds"));
        builder.Services.Configure<ConfigLeaderboardSettings>(builder.Configuration.GetSection("LeaderboardSettings"));
        builder.Services.Configure<FarmingItemsSettings>(builder.Configuration.GetSection("FarmingItems"));

        builder.Services.Configure<ConfigApiRateLimitSettings>(
            builder.Configuration.GetSection(ConfigApiRateLimitSettings.RateLimitName));

        builder.Configuration.GetSection(ConfigGlobalRateLimitSettings.RateLimitName)
            .Bind(ConfigGlobalRateLimitSettings.Settings);
    }

    public static void AddEliteRateLimiting(this IServiceCollection services) {
        var globalRateLimitSettings = ConfigGlobalRateLimitSettings.Settings;
            
        services.AddRateLimiter(limiterOptions => {
            limiterOptions.OnRejected = (context, _) => {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                //Log the rate limit rejection
                var logger = context.HttpContext.RequestServices.GetService<ILogger<ApiRateLimiterPolicy>>();
                logger?.LogWarning("Rate limit exceeded for {Ip}", context.HttpContext.Connection.RemoteIpAddress);
                
                return new ValueTask();
            };

            limiterOptions.AddPolicy<string, ApiRateLimiterPolicy>(ConfigApiRateLimitSettings.RateLimitName);
            
            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context => {
                var remoteIpAddress = context.Connection.RemoteIpAddress;

                // Check if IP address is from docker network
                if (remoteIpAddress is null 
                    || IPAddress.IsLoopback(remoteIpAddress) 
                    || remoteIpAddress.IsFromDockerNetwork())
                {
                    return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);
                }
                
                return RateLimitPartition.GetTokenBucketLimiter(remoteIpAddress, _ =>
                    new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = globalRateLimitSettings.TokenLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = globalRateLimitSettings.QueueLimit,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(globalRateLimitSettings.ReplenishmentPeriod),
                        TokensPerPeriod = globalRateLimitSettings.TokensPerPeriod,
                        AutoReplenishment = globalRateLimitSettings.AutoReplenishment
                    });
            });
        });
    }

    public static bool IsFromDockerNetwork(this IPAddress ip)
    {
        // Check if the IP address is from the Docker network or local.
        return IPAddress.IsLoopback(ip) || ip.ToString().StartsWith("172.") || ip.MapToIPv4().ToString().StartsWith("172.");
    }
}
