using FastEndpoints;
using System.Net;
using System.Text.Json;
using EliteAPI;
using EliteAPI.Authentication;
using EliteAPI.Background;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Utilities;
using FastEndpoints.Swagger;
using HypixelAPI;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.RegisterEliteConfigFiles();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => {
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services
    .AddEliteRedisCache()
    .AddFastEndpoints(o => {
        o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
    });

builder.Services.AddIdempotency();

builder.Services.SwaggerDocument(o => {
    o.DocumentSettings = doc => {
        doc.MarkNonNullablePropsAsRequired();
    };
});

builder.Services.AddEliteServices();
builder.Services.AddEliteAuthentication(builder.Configuration);
builder.Services.AddEliteControllers();
builder.Services.AddEliteScopedServices();
builder.Services.AddEliteRateLimiting();
builder.Services.AddEliteBackgroundJobs();

builder.Services.AddHypixelApi(DotNetEnv.Env.GetString("HYPIXEL_API_KEY"), "EliteAPI");

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

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();
app.UseForwardedHeaders();
app.UseResponseCompression();
app.UseRouting();
app.UseRateLimiter();

app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
app.MapScalarApiReference("/");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();
app.MapControllers();

app.UseFastEndpoints(o => {
    o.Binding.ReflectionCache.AddFromEliteAPI();
    o.Binding.UsePropertyNamingPolicy = true;
    o.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.Versioning.Prefix = "v";
    o.Versioning.PrependToRoute = true;

    o.Endpoints.Configurator = endpoints => {
        if (endpoints.IdempotencyOptions is not null) {
            endpoints.IdempotencyOptions.CacheDuration = TimeSpan.FromMinutes(1);
        } 
    };
});

app.Use(async (context, next) => {
    var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
    
    if (tagsFeature is not null) {
        var userAgent = context.Request.Headers.UserAgent.ToString() switch {
            var ua when ua.StartsWith("SkyHanni") => "SkyHanni",
            var ua when ua.StartsWith("Mozilla") => "Browser",
            var ua when ua.StartsWith("EliteWebsite") => "EliteWebsite",
            var ua when ua.StartsWith("EliteDiscordBot") => "EliteBot",
            _ => "Other"
        };
        
        tagsFeature.Tags.Add(new KeyValuePair<string, object?>("user_agent", userAgent));
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

    var logging = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logging.LogInformation("Starting EliteAPI...");

    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }
}

app.Run();