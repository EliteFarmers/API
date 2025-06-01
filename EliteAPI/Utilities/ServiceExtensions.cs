using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using EliteAPI.Authentication;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.RateLimiting;
using EliteAPI.Services;
using EliteAPI.Services.Background;
using EliteAPI.Services.Interfaces;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.Json;
using NuGet.Packaging;
using StackExchange.Redis;

namespace EliteAPI.Utilities;

public static class ServiceExtensions
{
    public static IServiceCollection AddEliteServices(this IServiceCollection services) {
        // Add AutoMapper
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Add services to the container.
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddSingleton<IObjectStorageService, ObjectStorageService>();

        services.AddHostedService<BackgroundQueueWorker>();
        
        services.AddHttpClient(HypixelService.HttpClientName, client => {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EliteAPI");
        });

        services.AddDbContext<DataContext>();
        
        // Not the best way to do this, but it works for now running on a single instance
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("TempKeys"))
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration() {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            });
        
        return services;
    }

    public static IServiceCollection AddEliteAuthentication(this IServiceCollection services,
        IConfiguration configuration) {
        var secret = configuration["Jwt:Secret"] ?? throw new Exception("Jwt:Secret is not set in app settings");

        services.AddScoped<IAuthorizationHandler, GuildAdminHandler>();

        services.AddIdentityCore<ApiUser>(o => {
                o.ClaimsIdentity.RoleClaimType = ClaimNames.Role;
                o.ClaimsIdentity.UserIdClaimType = ClaimNames.NameId;
                o.ClaimsIdentity.UserNameClaimType = ClaimNames.Name;
            })
            .AddRoles<IdentityRole>()
            .AddTokenProvider<DataProtectorTokenProvider<ApiUser>>("EliteAPI")
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<DataContext>();

        services.AddAuthenticationJwtBearer(options => {
            options.SigningKey = secret;
        });
        
        services.Configure<JwtCreationOptions>(options => {
            options.SigningKey = secret;
        });

        services.AddAuthorization(options => {
            options.AddPolicy(ApiUserPolicies.Admin, 
                policy => policy.RequireRole(ApiUserPolicies.Admin));
            options.AddPolicy(ApiUserPolicies.Moderator,
                policy => policy.RequireRole(ApiUserPolicies.Moderator, ApiUserPolicies.Admin));
            options.AddPolicy(ApiUserPolicies.Support,
                policy => policy.RequireRole(ApiUserPolicies.Support, ApiUserPolicies.Moderator, ApiUserPolicies.Admin));
            options.AddPolicy(ApiUserPolicies.Wiki,
                policy => policy.RequireRole(ApiUserPolicies.Wiki, ApiUserPolicies.Support, ApiUserPolicies.Moderator, ApiUserPolicies.Admin));
            options.AddPolicy(ApiUserPolicies.User, 
                policy => policy.RequireRole(ApiUserPolicies.User));
            options.AddGuildAdminPolicies();
        });

        return services;
    }

    public static IServiceCollection AddEliteScopedServices(this IServiceCollection services) {
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IHypixelService, HypixelService>();
        services.AddScoped<IMojangService, MojangService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDiscordService, DiscordService>();
        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<ITimescaleService, TimescaleService>();
        services.AddScoped<IBadgeService, BadgeService>();
        services.AddScoped<IMonetizationService, MonetizationService>();
        services.RegisterServicesFromEliteAPI();

        services.AddScoped<LocalOnlyMiddleware>();
        services.AddScoped<DiscordBotOnlyFilter>();
        
        return services;
    }

    public static IServiceCollection AddEliteRedisCache(this IServiceCollection services)
    {
        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6380";
        var config = new ConfigurationOptions {
            EndPoints = { redisConnection },
            Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
            AbortOnConnectFail = false,
            ConnectRetry = 5
        };
        var multiplexer = ConnectionMultiplexer.Connect(config);
        
        services
            .AddSingleton<IConnectionMultiplexer>(multiplexer)
            .AddOutputCache(options => {
                options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(10);
                
                options.AddPolicy(CachePolicy.Hours, policy => {
                    policy.Cache();
                    policy.Expire(TimeSpan.FromHours(4));
                    policy.Tag("hours");
                });
                
                options.AddPolicy(CachePolicy.NoCache, policy => {
                    policy.NoCache();
                });
            }).AddStackExchangeRedisOutputCache(options => {
                options.Configuration = config.ToString();
                options.InstanceName = "EliteAPI-OutputCache";
            }).AddStackExchangeRedisCache(options => {
                options.Configuration = config.ToString();
                options.InstanceName = "EliteAPI";
            });
        
        return services;
    }
    
    public static IConfigurationBuilder RegisterEliteConfigFiles(this IConfigurationBuilder configurationBuilder, string directoryPath = "Configuration")
    {
        configurationBuilder.Sources.AddRange(new List<JsonConfigurationSource>
        {
            new() { Path = $"{directoryPath}/Weight.json", ReloadOnChange = true, Optional = false },
            new() { Path = $"{directoryPath}/Cooldown.json", ReloadOnChange = true, Optional = false },
            new() { Path = $"{directoryPath}/Leaderboards.json", ReloadOnChange = true, Optional = false },
            new() { Path = $"{directoryPath}/Farming.json", ReloadOnChange = true, Optional = false },
            new() { Path = $"{directoryPath}/ChocolateFactory.json", ReloadOnChange = true, Optional = false },
            new() { Path = $"{directoryPath}/Events.json", ReloadOnChange = true, Optional = false },
            new() { Path = $"{directoryPath}/Pets.json", ReloadOnChange = true, Optional = false },
        });
        
        return configurationBuilder;
    }

    public static WebApplicationBuilder RegisterEliteConfigFiles(this WebApplicationBuilder builder)
    {
        builder.Configuration.RegisterEliteConfigFiles();

        builder.Services.Configure<ConfigFarmingWeightSettings>(builder.Configuration.GetSection("FarmingWeight"));
        builder.Services.Configure<ConfigCooldownSettings>(builder.Configuration.GetSection("CooldownSeconds"));
        builder.Services.Configure<ConfigLeaderboardSettings>(builder.Configuration.GetSection("LeaderboardSettings"));
        builder.Services.Configure<FarmingItemsSettings>(builder.Configuration.GetSection("Farming"));
        builder.Services.Configure<ChocolateFactorySettings>(builder.Configuration.GetSection("ChocolateFactory"));
        builder.Services.Configure<MessagingSettings>(builder.Configuration.GetSection("Messaging"));
        builder.Services.Configure<ConfigEventSettings>(builder.Configuration.GetSection("Events"));
        builder.Services.Configure<SkyblockPetSettings>(builder.Configuration.GetSection("Pets"));

        builder.Services.Configure<ConfigApiRateLimitSettings>(
            builder.Configuration.GetSection(ConfigApiRateLimitSettings.RateLimitName));

        builder.Configuration.GetSection(ConfigGlobalRateLimitSettings.RateLimitName)
            .Bind(ConfigGlobalRateLimitSettings.Settings);
        
        return builder;
    }

    public static IServiceCollection AddEliteRateLimiting(this IServiceCollection services) {
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
        
        return services;
    }

    public static bool IsFromDockerNetwork(this IPAddress ip)
    {
        // Check if the IP address is from the Docker network or local.
        return IPAddress.IsLoopback(ip) || ip.ToString().StartsWith("172.") || ip.MapToIPv4().ToString().StartsWith("172.");
    }
}

public static class CachePolicy {
    public const string NoCache = "NoCache";
    public const string Hours = "Hours";
}