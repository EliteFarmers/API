using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using EliteAPI.Authentication;
using EliteAPI.Configuration.Settings;
using EliteAPI.Configuration.Swagger;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Parsers.Skyblock;
using EliteAPI.RateLimiting;
using EliteAPI.Services;
using EliteAPI.Services.Background;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EliteAPI.Utilities;

public static class ServiceExtensions
{
    public static void AddEliteServices(this IServiceCollection services) {
        // Add AutoMapper
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Add services to the container.
        services.AddSingleton<HypixelRequestLimiter>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        services.AddHostedService<BackgroundQueueWorker>();
        
        services.AddHttpClient(HypixelService.HttpClientName, client => {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EliteAPI");
        });

        services.AddDbContext<DataContext>();
    }
    
    public static void AddEliteAuthentication(this IServiceCollection services, IConfiguration configuration) {
        var secret = configuration["Jwt:Secret"] ?? throw new Exception("Jwt:Secret is not set in app settings");

        services.AddScoped<IAuthorizationHandler, GuildAdminHandler>();
        
        services.AddIdentityCore<ApiUser>()
            .AddRoles<IdentityRole>()
            .AddTokenProvider<DataProtectorTokenProvider<ApiUser>>("EliteAPI")
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<DataContext>();
        
        services.AddAuthentication(options => 
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
            .AddPolicy("Moderator", policy => policy.RequireRole("Moderator", "Admin"))
            .AddPolicy("Support", policy => policy.RequireRole("Support", "Moderator", "Admin"))
            .AddPolicy("Wiki", policy => policy.RequireRole("Wiki", "Support", "Moderator", "Admin"))
            .AddPolicy("User", policy => policy.RequireRole("User"))
            .AddGuildAdminPolicies();
    }

    public static void AddEliteControllers(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddApiVersioning(options => {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options => {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddTransient<IDocumentFilter, DefaultApiVersionFilter>();
        services.AddSwaggerGen();
    }

    public static void AddEliteScopedServices(this IServiceCollection services) {
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IHypixelService, HypixelService>();
        services.AddScoped<IMojangService, MojangService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDiscordService, DiscordService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<ITimescaleService, TimescaleService>();
        services.AddScoped<IBadgeService, BadgeService>();
        services.AddScoped<IEventService, EventService>();

        services.AddScoped<ProfileParser>();
        services.AddScoped<DiscordBotOnlyFilter>();
    }

    public static void AddEliteRedisCache(this IServiceCollection services)
    {
        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6380";
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
        builder.Configuration.Sources.AddRange(new List<JsonConfigurationSource>
        {
            new() { Path = "Configuration/Weight.json" },
            new() { Path = "Configuration/Cooldown.json" },
            new() { Path = "Configuration/Leaderboards.json" },
            new() { Path = "Configuration/Farming.json" },
            new() { Path = "Configuration/ChocolateFactory.json" }
        });

        builder.Services.Configure<ConfigFarmingWeightSettings>(builder.Configuration.GetSection("FarmingWeight"));
        builder.Services.Configure<ConfigCooldownSettings>(builder.Configuration.GetSection("CooldownSeconds"));
        builder.Services.Configure<ConfigLeaderboardSettings>(builder.Configuration.GetSection("LeaderboardSettings"));
        builder.Services.Configure<FarmingItemsSettings>(builder.Configuration.GetSection("Farming"));
        builder.Services.Configure<ChocolateFactorySettings>(builder.Configuration.GetSection("ChocolateFactory"));
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

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
