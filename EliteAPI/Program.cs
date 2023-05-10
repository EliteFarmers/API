global using EliteAPI.Data.Models;

using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Services.AccountService;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<MetricsService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddDbContext<DataContext>();

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
    while (true)
    {
        MetricsService.IncrementRequestCount("GET", "/api/v1/account", "200", "10");
        Thread.Sleep(1000);
    }
}).Start();

app.Run();