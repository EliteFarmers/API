using System.Net;
using EliteAPI.Authentication;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.RegisterEliteConfigFiles();

builder.Services.AddEliteServices();
builder.Services.AddEliteControllers();
builder.Services.AddEliteRedisCache();
builder.Services.AddEliteScopedServices();
builder.Services.AddEliteRateLimiting();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddScoped<LocalOnlyMiddleware>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(x =>
    {
        x.AddPrometheusExporter();

        x.AddMeter("System.Runtime", "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel");
        x.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
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

app.UseSwagger(opt => {
    opt.RouteTemplate = "{documentName}/swagger.json";
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Use(async (context, next) => {
    var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
    
    if (tagsFeature is not null) {
        var userAgent = context.Request.Headers.UserAgent.ToString() switch {
            var ua when ua.StartsWith("SkyHanni") => "SkyHanni",
            var ua when ua.StartsWith("Mozilla") => "Browser",
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
        if (db.Database.EnsureCreated()) {
            db.ApplyHyperTables();
        }
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }
}

app.Run();