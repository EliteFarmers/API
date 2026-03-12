global using UserManager = Microsoft.AspNetCore.Identity.UserManager<EliteAPI.Features.Auth.Models.ApiUser>;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using EliteAPI.Authentication;
using EliteAPI.Background;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Auth.Services;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Setup;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using SkyblockRepo;
using IPNetwork = System.Net.IPNetwork;
#if !DEBUG
using OpenTelemetry.Trace;
using Pyroscope.OpenTelemetry;
#endif

[assembly: InternalsVisibleTo("EliteAPI.Tests")]

var doctorMode = args.Any(arg =>
	arg.Equals("doctor", StringComparison.OrdinalIgnoreCase) ||
	arg.Equals("--doctor", StringComparison.OrdinalIgnoreCase));

var builder = WebApplication.CreateBuilder(args);
var hypixelSettings = builder.Configuration.GetSection(HypixelSettings.SectionName).Get<HypixelSettings>() ??
                      new HypixelSettings();
var rendererSettings =
	builder.Configuration.GetSection(MinecraftRendererSettings.SectionName).Get<MinecraftRendererSettings>() ??
	new MinecraftRendererSettings();

builder.RegisterEliteConfigFiles();
builder.Services.AddEliteAuthentication(builder.Configuration);

builder.Services.AddEliteRedisCache(builder.Configuration);
builder.Services.AddIdempotency();
builder.Services.AddResponseCaching();

builder.Services.AddEliteSwaggerDocumentation();

builder.Services.AddEliteServices();
builder.Services.AddEliteScopedServices();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEliteRateLimiting();
builder.AddEliteBackgroundJobs();

builder.Services.AddHypixelApi(opt => {
	opt.ApiKey = hypixelSettings.ApiKey;
	opt.UserAgent = "EliteAPI (+https://api.eliteskyblock.com)";
}).AddStandardResilienceHandler();

builder.Services.AddRouting(options => {
	options.LowercaseUrls = true;
	options.LowercaseQueryStrings = true;
});

builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

const int hundredMb = 100 * 1024 * 1024;

builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = hundredMb; });

builder.Services.Configure<FormOptions>(options => {
	options.ValueLengthLimit = hundredMb;
	options.MultipartBodyLengthLimit = hundredMb;
	options.MultipartHeadersLengthLimit = hundredMb;
});

builder.Services.AddOpenTelemetry()
	.WithMetrics(x => {
		x.AddPrometheusExporter();

		x.AddMeter("System.Runtime", "Microsoft.AspNetCore.Hosting",
			"Microsoft.AspNetCore.Server.Kestrel");
		x.AddView("http.server.request.duration",
			new ExplicitBucketHistogramConfiguration {
				Boundaries = [
					0, 0.005, 0.01, 0.025, 0.05,
					0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10
				]
			});

		x.AddMeter("hypixel.api");
		x.AddMeter("eliteapi.leaderboard");
		x.AddMeter("eliteapi.update_path");
	})
	.WithTracing(x => {
		x.AddSource("EliteAPI");
#if !DEBUG
		x.AddProcessor(new PyroscopeSpanProcessor());
#endif
	});

// Use Cloudflare IP address as the client remote IP address
builder.Services.Configure<ForwardedHeadersOptions>(opt => {
	opt.ForwardedForHeaderName = "CF-Connecting-IP";
	opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
	// Safe because we only allow Cloudflare to connect to the API through the firewall
	opt.KnownIPNetworks.Add(new IPNetwork(IPAddress.Any, 0));
	opt.KnownIPNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
});

builder.AddEliteFastEndpoints();

builder.Services.AddSkyblockRepo(opt => {
	opt.UseNeuRepo = true;
	opt.FileStoragePath = rendererSettings.ResolveAssetsPath();
	opt.Matcher.Register(new EliteItemRepoMatcher());
	opt.Matcher.Register(new RenderContextRepoMatcher());
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope()) {
	var setupDoctor = scope.ServiceProvider.GetRequiredService<IEliteSetupDoctor>();
	var report = await setupDoctor.CreateReportAsync(includeConnectivityChecks: true);

	if (doctorMode) {
		setupDoctor.WriteReport(Console.Out, report);
		Environment.ExitCode = report.HasErrors ? 1 : 0;
		return;
	}

	var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
	setupDoctor.LogReport(startupLogger, report);

	if (report.HasErrors) {
		throw new InvalidOperationException(
			"Startup configuration validation failed. Run `dotnet run --project EliteAPI -- doctor` for details.");
	}
}

using (var scope = app.Services.CreateScope()) {
	FarmingWeightConfig.Settings =
		scope.ServiceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;
	FarmingItemsConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<FarmingItemsSettings>>().Value;
	SkyblockPetConfig.Settings = scope.ServiceProvider.GetRequiredService<IOptions<SkyblockPetSettings>>().Value;
	ConfigGlobalRateLimitSettings.Settings =
		scope.ServiceProvider.GetRequiredService<IOptions<ConfigGlobalRateLimitSettings>>().Value;
	
	var db = scope.ServiceProvider.GetRequiredService<DataContext>();
	await db.Database.MigrateAsync();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions {
	Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions {
	Predicate = registration => registration.Tags.Contains("ready")
});

app.MapPrometheusScrapingEndpoint();
app.UseForwardedHeaders();

app.Use(async (context, next) => {
	var settings = context.RequestServices.GetRequiredService<IOptions<SetupDiagnosticsSettings>>().Value;
	if (!settings.LogProfileRequests ||
	    settings.PathPrefixes.All(prefix => !context.Request.Path.StartsWithSegments(prefix))) {
		await next();
		return;
	}

	var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
		.CreateLogger("EliteAPI.Setup.RequestDiagnostics");
	var stopwatch = Stopwatch.StartNew();
	logger.LogInformation("Setup diagnostic request started: {Method} {Path}{QueryString}",
		context.Request.Method,
		context.Request.Path,
		context.Request.QueryString);

	try {
		await next();
		logger.LogInformation("Setup diagnostic request completed: {Method} {Path} -> {StatusCode} in {ElapsedMs}ms",
			context.Request.Method,
			context.Request.Path,
			context.Response.StatusCode,
			stopwatch.ElapsedMilliseconds);
	}
	catch (Exception ex) {
		logger.LogError(ex, "Setup diagnostic request failed: {Method} {Path} after {ElapsedMs}ms",
			context.Request.Method,
			context.Request.Path,
			stopwatch.ElapsedMilliseconds);
		throw;
	}
});

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

		if (userAgentGroup == skyHanni) {
			var parts = userAgentHeader.Split('/');
			var version = parts.Length > 1 ? parts[1] : unknownString;

			if (version.Contains('-')) {
				var versionParts = version.Split('-');
				version = versionParts[0];
				var mcVersion = versionParts.Length > 1 ? versionParts[1] : unknownString;

				metricTags.Tags.Add(new KeyValuePair<string, object?>(skyHanniMcVersion, mcVersion));
			}

			context.Items["skyhanni_version"] = version;
			metricTags.Tags.Add(new KeyValuePair<string, object?>(skyHanniVersion, version));
		}

		metricTags.Tags.Add(new KeyValuePair<string, object?>(tagName, userAgentGroup));

		if (context.Request.Headers.TryGetValue("X-Known-Bot", out var bot))
			if (bot.Count > 0 && bot[0]!.Equals("true", StringComparison.OrdinalIgnoreCase)) {
				metricTags.Tags.Add(new KeyValuePair<string, object?>("known_bot", "1"));
				context.Items["known_bot"] = true;
			}
	}

	await next.Invoke();
});

// Secure the metrics endpoint
app.UseWhen(context => context.Request.Path.StartsWithSegments("/metrics"),
	applicationBuilder => { applicationBuilder.UseMiddleware<LocalOnlyMiddleware>(); });

using (var scope = app.Services.CreateScope()) {
	var logging = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
	logging.LogInformation("Starting EliteAPI...");

	var repo = scope.ServiceProvider.GetRequiredService<ISkyblockRepoClient>();
	await repo.InitializeAsync();

	var lbRegistration = scope.ServiceProvider.GetRequiredService<ILeaderboardRegistrationService>();
	await lbRegistration.RegisterLeaderboardsAsync(CancellationToken.None);

	var adminSeeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();
	await adminSeeder.SeedAdminUserAsync();
}

app.Run();
