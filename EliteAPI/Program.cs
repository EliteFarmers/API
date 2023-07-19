using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prometheus;

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

builder.Services.Configure<ForwardedHeadersOptions>(opt => {
    opt.ForwardedForHeaderName = "CF-Connecting-IP";
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseResponseCompression();
app.UseRouting();
app.UseRateLimiter();

app.UseMetricServer(9102);
app.UseHttpMetrics();

app.UseSwagger(opt => {
    opt.RouteTemplate = "{documentName}/swagger.json";
});
app.UseSwaggerUI(opt => {
    opt.RoutePrefix = "";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

using (var scope = app.Services.CreateScope())
{
    FarmingWeightConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;

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