global using UserManager = Microsoft.AspNetCore.Identity.UserManager<EliteAPI.Features.Auth.Models.ApiUser>;
using FastEndpoints;
using System.Net;
using System.Runtime.CompilerServices;
using EliteAPI;
using EliteAPI.Authentication;
using EliteAPI.Background;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Textures.Services;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using SkyblockRepo;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
[assembly: InternalsVisibleTo("Tests")]

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.RegisterEliteConfigFiles();
builder.Services.AddEliteAuthentication(builder.Configuration);

builder.Services.AddEliteRedisCache();
builder.Services.AddIdempotency();
builder.Services.AddResponseCaching();

builder.Services.AddEliteSwaggerDocumentation();

builder.Services.AddEliteServices();
builder.Services.AddEliteScopedServices();
builder.Services.AddEliteRateLimiting();
builder.Services.AddEliteBackgroundJobs();

builder.Services.AddHypixelApi(opt => {
    opt.ApiKey = DotNetEnv.Env.GetString("HYPIXEL_API_KEY");
    opt.UserAgent = "EliteAPI (+https://api.eliteapi.dev)";
}).AddStandardResilienceHandler();

builder.Services.AddRouting(options => {
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

const int hundredMb = 100 * 1024 * 1024;

builder.Services.Configure<KestrelServerOptions>(options => {
    options.Limits.MaxRequestBodySize = hundredMb;
});

builder.Services.Configure<FormOptions>(options => {
    options.ValueLengthLimit = hundredMb;
    options.MultipartBodyLengthLimit = hundredMb;
    options.MultipartHeadersLengthLimit = hundredMb;
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(x =>
    {
        x.AddPrometheusExporter();

        x.AddMeter("System.Runtime", "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel");
        x.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = [
                    0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10
                ]
            });
        
        x.AddMeter("hypixel.api");
    });

// Use Cloudflare IP address as the client remote IP address
builder.Services.Configure<ForwardedHeadersOptions>(opt => {
    opt.ForwardedForHeaderName = "CF-Connecting-IP";
    opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    // Safe because we only allow Cloudflare to connect to the API through the firewall
    opt.KnownNetworks.Add(new IPNetwork(IPAddress.Any, 0));
    opt.KnownNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
});

builder.Services.AddFastEndpoints(o => {
    o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
});

builder.Services.AddSkyblockRepo(opt =>
{
    opt.UseNeuRepo = true;
});

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();
app.UseForwardedHeaders();
app.UseEliteRateLimiting();

app.UseResponseCaching();
app.UseResponseCompression();
app.UseRouting();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.UseDefaultExceptionHandler();

app.UseEliteFastEndpoints();

app.UseEliteOpenApi();

app.Use(async (context, next) => {
    const string skyHanni = "SkyHanni";
    const string eliteWebsiteUserAgent = "EliteWebsite";
    const string eliteDiscordBotUserAgent = "EliteDiscordBot";
    const string eliteBotString = "EliteBot";
    const string browserUserAgent = "Mozilla";
    const string browserString = "Browser";
    const string otherString = "Other";
    const string unknownString = "unknown";
    
    const string skyHanniVersion = "sh_version";
    const string skyHanniMcVersion = "mc_version";
    const string tagName = "user_agent";
    
    var metricTags = context.Features.Get<IHttpMetricsTagsFeature>();
    if (metricTags is not null) {
        var userAgentHeader = context.Request.Headers.UserAgent.ToString();
        
        var userAgentGroup = userAgentHeader switch {
            _ when userAgentHeader.StartsWith(skyHanni) => skyHanni,
            _ when userAgentHeader.StartsWith(browserUserAgent) => browserString,
            _ when userAgentHeader.StartsWith(eliteWebsiteUserAgent) => eliteWebsiteUserAgent,
            _ when userAgentHeader.StartsWith(eliteDiscordBotUserAgent) => eliteBotString,
            _ => otherString
        };

        if (userAgentGroup == skyHanni)
        {
            var parts = userAgentHeader.Split('/');
            var version = parts.Length > 1 ? parts[1] : unknownString;
            
            if (version.Contains('-'))
            {
                var versionParts = version.Split('-');
                version = versionParts[0];
                var mcVersion = versionParts.Length > 1 ? versionParts[1] : unknownString;
            
                metricTags.Tags.Add(new KeyValuePair<string, object?>(skyHanniMcVersion, mcVersion));
            }
            
            metricTags.Tags.Add(new KeyValuePair<string, object?>(skyHanniVersion, version));
        }
        
        metricTags.Tags.Add(new KeyValuePair<string, object?>(tagName, userAgentGroup));

        if (context.Request.Headers.TryGetValue("X-Known-Bot", out var bot))
        {
            if (bot.Count > 0 && bot[0]!.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                metricTags.Tags.Add(new KeyValuePair<string, object?>("known_bot", "1"));
                context.Items["known_bot"] = true;
            }
        }
    }

    await next.Invoke();
});

// Secure the metrics endpoint
app.UseWhen(context => context.Request.Path.StartsWithSegments("/metrics"), applicationBuilder => {
    applicationBuilder.UseMiddleware<LocalOnlyMiddleware>();
});

using (var scope = app.Services.CreateScope())
{
    FarmingWeightConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;
    FarmingItemsConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<FarmingItemsSettings>>().Value;
    SkyblockPetConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<SkyblockPetSettings>>().Value;

    var logging = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logging.LogInformation("Starting EliteAPI...");
    
    var repo = scope.ServiceProvider.GetRequiredService<ISkyblockRepoClient>();
    await repo.InitializeAsync();
    
    await RendererConfiguration.DownloadMinecraftTexturesAsync(builder.Configuration);
    
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }
    
    var lbRegistration = scope.ServiceProvider.GetRequiredService<ILeaderboardRegistrationService>();
    await lbRegistration.RegisterLeaderboardsAsync(CancellationToken.None);
}

app.Run();