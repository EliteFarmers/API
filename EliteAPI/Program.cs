using System.Net;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

// Use Cloudflare IP address as the client remote IP address
builder.Services.Configure<ForwardedHeadersOptions>(opt => {
    opt.ForwardedForHeaderName = "CF-Connecting-IP";
    opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    // Safe because we only allow Cloudflare to connect to the API through the firewall
    opt.KnownNetworks.Add(new IPNetwork(IPAddress.Any, 0));
    opt.KnownNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseResponseCompression();
app.UseRouting();
app.UseRateLimiter();

//app.UseMetricServer(9102);
//app.UseHttpMetrics();

app.UseSwagger(opt => {
    opt.RouteTemplate = "{documentName}/swagger.json";
});
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    FarmingWeightConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;
    FarmingItemsConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<FarmingItemsSettings>>().Value;

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