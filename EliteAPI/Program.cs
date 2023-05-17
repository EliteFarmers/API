global using EliteAPI.Data.Models;

using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Services.AccountService;
using EliteAPI.Services.ContestService;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using EliteAPI.Transformers.Skyblock;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<IHypixelService, HypixelService>();
builder.Services.AddSingleton<IMojangService, MojangService>();
builder.Services.AddSingleton<ProfilesTransformer>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IContestService, ContestService>();


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