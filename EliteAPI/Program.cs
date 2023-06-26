using EliteAPI.Authentication;
using EliteAPI.Config;
using EliteAPI.Data;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Services;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using EliteAPI.Services.ProfileService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Prometheus;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add services to the container.
builder.Services.AddSingleton<MetricsService>();

builder.Services.AddHttpClient(HypixelService.HttpClientName, client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("EliteAPI");
}).UseHttpClientMetrics();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>();

builder.Services.AddScoped<IHypixelService, HypixelService>();
builder.Services.AddScoped<IMojangService, MojangService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IDiscordService, DiscordService>();

builder.Services.AddScoped<ProfileParser>();

builder.Services.AddScoped<DiscordAuthFilter>();

builder.Configuration.Sources.Add(new JsonConfigurationSource()
{
    Path = "Config\\Weight.json",
});

builder.Services.Configure<ConfigFarmingWeightSettings>(builder.Configuration.GetSection("FarmingWeight"));

var app = builder.Build();

app.UseMetricServer(9102);
app.UseRouting();
app.UseHttpMetrics();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }
}

app.Run();