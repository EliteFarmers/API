using EliteAPI.Data;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Services;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using EliteAPI.Services.ProfileService;
using Microsoft.EntityFrameworkCore;
using Prometheus;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add services to the container.
builder.Services.AddSingleton<MetricsService>();

var client = builder.Services.AddHttpClient(HypixelService.HttpClientName, client =>
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

builder.Services.AddScoped<ProfileParser>();

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

app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

using (var scope = app.Services.CreateScope())
{
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

new Task(() =>
{
    Thread.Sleep(3000);
    
    while (true)
    {
        MetricsService.IncrementRequestCount("GET", "/api/v1/account", "200", "10");
        Thread.Sleep(1000);
    }
}).Start();

app.Run();