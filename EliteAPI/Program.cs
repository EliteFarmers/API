global using EliteAPI.Data.Models;

using EliteAPI.Data;
using EliteAPI.Services.AccountService;
using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Instrumentation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
Meter meter = new("EliteAPI", "1.0");
Counter<long> counter = meter.CreateCounter<long>("RequestCounter");

// Add services to the container.
builder.Services.AddSingleton<Instrumentation>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.AddOpenTelemetry();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddDbContext<DataContext>();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "Elite.API", serviceVersion: "1.0", serviceInstanceId: Environment.MachineName))
    .WithTracing(tracer => tracer
        .AddSource(Instrumentation.ActivitySourceName)
        .SetSampler(new AlwaysOnSampler())
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(Instrumentation.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddPrometheusHttpListener(options => options
            .UriPrefixes = new string[] { "http://localhost:9464/" }));

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter(Instrumentation.MeterName)
    .SetExemplarFilter(new TraceBasedExemplarFilter())
    .AddPrometheusHttpListener()
    .Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();

    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
}*/

//app.UseOpenTelemetryPrometheusScrapingEndpoint();

new Task(() =>
{
    while (true)
    {
        counter.Add(1, new("Test", "data"), new("Wia", "100"));
        Thread.Sleep(1000);
    }
});

app.Run();