using System.Net;
using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;

namespace EliteAPI.Setup;

public enum EliteSetupSeverity
{
	Info,
	Warning,
	Error
}

public sealed record EliteSetupMessage(EliteSetupSeverity Severity, string Message);

public sealed class EliteSetupReport
{
	public string EnvironmentName { get; set; } = string.Empty;
	public string RuntimeMode { get; set; } = string.Empty;
	public string ExpectedApiUrl { get; set; } = string.Empty;
	public string PostgresTarget { get; set; } = string.Empty;
	public string RedisTarget { get; set; } = string.Empty;
	public bool WebsiteSecretConfigured { get; set; }

	public List<EliteSetupMessage> Messages { get; } = [];

	public bool HasErrors => Messages.Any(m => m.Severity == EliteSetupSeverity.Error);
	public bool HasWarnings => Messages.Any(m => m.Severity == EliteSetupSeverity.Warning);

	public void AddInfo(string message) => Messages.Add(new EliteSetupMessage(EliteSetupSeverity.Info, message));
	public void AddWarning(string message) => Messages.Add(new EliteSetupMessage(EliteSetupSeverity.Warning, message));
	public void AddError(string message) => Messages.Add(new EliteSetupMessage(EliteSetupSeverity.Error, message));
}

public interface IEliteSetupDoctor
{
	Task<EliteSetupReport> CreateReportAsync(bool includeConnectivityChecks, CancellationToken ct = default);
	void LogReport(ILogger logger, EliteSetupReport report);
	void WriteReport(TextWriter writer, EliteSetupReport report);
}

public sealed class EliteSetupDoctor(
	IConfiguration configuration,
	IHostEnvironment environment,
	IOptions<DiscordSettings> discordOptions,
	IOptions<HypixelSettings> hypixelOptions,
	IOptions<JwtSettings> jwtOptions,
	IOptions<WebsiteGatewaySettings> websiteOptions,
	IOptions<MinecraftRendererSettings> rendererOptions)
	: IEliteSetupDoctor
{
	private readonly DiscordSettings _discord = discordOptions.Value;
	private readonly HypixelSettings _hypixel = hypixelOptions.Value;
	private readonly JwtSettings _jwt = jwtOptions.Value;
	private readonly WebsiteGatewaySettings _website = websiteOptions.Value;
	private readonly MinecraftRendererSettings _rendererSettings = rendererOptions.Value;

	public async Task<EliteSetupReport> CreateReportAsync(bool includeConnectivityChecks, CancellationToken ct = default) {
		var report = new EliteSetupReport {
			EnvironmentName = environment.EnvironmentName,
			RuntimeMode = IsRunningInContainer() ? "full-stack container" : "local host",
			ExpectedApiUrl = ResolveExpectedApiUrl(),
			WebsiteSecretConfigured = !string.IsNullOrWhiteSpace(_website.WebsiteSecret)
		};

		if (environment.IsEnvironment("Testing")) {
			report.RuntimeMode = "testing";
			report.PostgresTarget = "(skipped in testing)";
			report.RedisTarget = "(skipped in testing)";
			report.AddInfo("Setup validation skipped for the Testing environment.");
			return report;
		}

		var postgresConnectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;
		var redisConnectionString = configuration.GetConnectionString("Redis") ?? string.Empty;

		var postgresBuilder = TryParsePostgresConnectionString(postgresConnectionString, report);
		var redisConfiguration = TryParseRedisConnectionString(redisConnectionString, report);

		report.PostgresTarget = postgresBuilder is null
			? "(invalid)"
			: $"{postgresBuilder.Host}:{postgresBuilder.Port}/{postgresBuilder.Database}";

		report.RedisTarget = redisConfiguration is null
			? "(invalid)"
			: GetRedisEndpoint(redisConfiguration) ?? "(unknown endpoint)";

		ValidateRequiredSetting(_discord.ClientId, "Discord__ClientId", report);
		ValidateRequiredSetting(_discord.ClientSecret, "Discord__ClientSecret", report);
		ValidateRequiredSetting(_discord.BotToken, "Discord__BotToken", report);
		ValidateRequiredSetting(_hypixel.ApiKey, "Hypixel__ApiKey", report);
		ValidateRequiredSetting(_jwt.Secret, "Jwt__Secret / Jwt:Secret", report);
		ValidateRequiredSetting(_website.WebsiteSecret, "WebsiteSecret", report);
		ValidateRequiredSetting(_rendererSettings.AcceptEula, "MinecraftRenderer__AcceptEula", report);
		
		ValidateRuntimeTargets(postgresBuilder, redisConfiguration, report);

		report.AddInfo($"Environment: {report.EnvironmentName}");
		report.AddInfo($"Runtime mode: {report.RuntimeMode}");
		report.AddInfo($"Expected local API URL: {report.ExpectedApiUrl}");
		report.AddInfo($"Postgres target: {report.PostgresTarget}");
		report.AddInfo($"Redis target: {report.RedisTarget}");
		report.AddInfo($"Website secret configured: {(report.WebsiteSecretConfigured ? "yes" : "no")}");

		if (!includeConnectivityChecks || report.HasErrors) {
			return report;
		}

		await ValidatePostgresConnectivityAsync(postgresBuilder!, report, ct);
		await ValidateRedisConnectivityAsync(redisConfiguration!, report, ct);

		return report;
	}

	public void LogReport(ILogger logger, EliteSetupReport report) {
		logger.LogInformation(
			"Setup summary: env={EnvironmentName}, mode={RuntimeMode}, apiUrl={ExpectedApiUrl}, postgres={PostgresTarget}, redis={RedisTarget}, websiteSecret={WebsiteSecretConfigured}",
			report.EnvironmentName,
			report.RuntimeMode,
			report.ExpectedApiUrl,
			report.PostgresTarget,
			report.RedisTarget,
			report.WebsiteSecretConfigured ? "configured" : "missing");

		foreach (var message in report.Messages) {
			switch (message.Severity) {
				case EliteSetupSeverity.Info:
					logger.LogInformation("{Message}", message.Message);
					break;
				case EliteSetupSeverity.Warning:
					logger.LogWarning("{Message}", message.Message);
					break;
				case EliteSetupSeverity.Error:
					logger.LogError("{Message}", message.Message);
					break;
				default:
					logger.LogInformation("{Message}", message.Message);
					break;
			}
		}
	}

	public void WriteReport(TextWriter writer, EliteSetupReport report) {
		writer.WriteLine($"Environment: {report.EnvironmentName}");
		writer.WriteLine($"Runtime mode: {report.RuntimeMode}");
		writer.WriteLine($"Expected local API URL: {report.ExpectedApiUrl}");
		writer.WriteLine($"Postgres target: {report.PostgresTarget}");
		writer.WriteLine($"Redis target: {report.RedisTarget}");
		writer.WriteLine($"Website secret configured: {(report.WebsiteSecretConfigured ? "yes" : "no")}");
		writer.WriteLine();

		foreach (var severity in new[] { EliteSetupSeverity.Error, EliteSetupSeverity.Warning, EliteSetupSeverity.Info }) {
			var messages = report.Messages.Where(m => m.Severity == severity).ToList();
			if (messages.Count == 0) {
				continue;
			}

			writer.WriteLine($"{severity}:");
			foreach (var message in messages) {
				writer.WriteLine($"- {message.Message}");
			}

			writer.WriteLine();
		}
	}

	private static NpgsqlConnectionStringBuilder? TryParsePostgresConnectionString(string connectionString,
		EliteSetupReport report) {
		if (string.IsNullOrWhiteSpace(connectionString)) {
			report.AddError("ConnectionStrings:Postgres is missing.");
			return null;
		}

		try {
			return new NpgsqlConnectionStringBuilder(connectionString);
		}
		catch (Exception ex) {
			report.AddError($"ConnectionStrings:Postgres is invalid: {ex.Message}");
			return null;
		}
	}

	private static ConfigurationOptions? TryParseRedisConnectionString(string connectionString, EliteSetupReport report) {
		if (string.IsNullOrWhiteSpace(connectionString)) {
			report.AddError("ConnectionStrings:Redis is missing.");
			return null;
		}

		try {
			return ConfigurationOptions.Parse(connectionString);
		}
		catch (Exception ex) {
			report.AddError($"ConnectionStrings:Redis is invalid: {ex.Message}");
			return null;
		}
	}

	private void ValidateRuntimeTargets(NpgsqlConnectionStringBuilder? postgresBuilder,
		ConfigurationOptions? redisConfiguration, EliteSetupReport report) {
		if (postgresBuilder?.Host is not null) {
			var host = postgresBuilder.Host;
			if (IsRunningInContainer()) {
				if (IsLoopbackHost(host)) {
					report.AddError(
						"Containerized API cannot use localhost for Postgres. Use pgbouncer:5432 via ConnectionStrings__Postgres.");
				}
				else if (!host.Equals("pgbouncer", StringComparison.OrdinalIgnoreCase)) {
					report.AddWarning(
						$"Supported full-stack flow uses pgbouncer for Postgres, but ConnectionStrings:Postgres points to {host}:{postgresBuilder.Port}.");
				}
			}
			else if (host.Equals("database", StringComparison.OrdinalIgnoreCase) ||
			         host.Equals("pgbouncer", StringComparison.OrdinalIgnoreCase)) {
				report.AddError(
					"Local host API flow should use localhost:5436 for Postgres, not the Docker-only hostnames database/pgbouncer.");
			}
		}

		if (redisConfiguration is not null) {
			var host = GetRedisHost(redisConfiguration);
			if (host is null) {
				report.AddWarning("ConnectionStrings:Redis did not resolve to a DNS endpoint.");
				return;
			}

			if (IsRunningInContainer()) {
				if (IsLoopbackHost(host)) {
					report.AddError(
						"Containerized API cannot use localhost for Redis. Use cache:6379 via ConnectionStrings__Redis.");
				}
				else if (!host.Equals("cache", StringComparison.OrdinalIgnoreCase)) {
					report.AddWarning(
						$"Supported full-stack flow uses cache for Redis, but ConnectionStrings:Redis points to {host}.");
				}
			}
			else if (host.Equals("cache", StringComparison.OrdinalIgnoreCase)) {
				report.AddError("Local host API flow should use localhost:6380 for Redis, not the Docker-only cache hostname.");
			}
		}
	}

	private async Task ValidatePostgresConnectivityAsync(NpgsqlConnectionStringBuilder postgresBuilder,
		EliteSetupReport report, CancellationToken ct) {
		var builder = new NpgsqlConnectionStringBuilder(postgresBuilder.ConnectionString) {
			Timeout = 5,
			CommandTimeout = 5
		};

		try {
			await using var connection = new NpgsqlConnection(builder.ConnectionString);
			await connection.OpenAsync(ct);
			await connection.CloseAsync();
			report.AddInfo("Postgres connectivity check passed.");
		}
		catch (Exception ex) {
			report.AddError(
				$"Postgres is not reachable at {postgresBuilder.Host}:{postgresBuilder.Port}. {GetStartupHint("Postgres")} Details: {ex.Message}");
		}
	}

	private async Task ValidateRedisConnectivityAsync(ConfigurationOptions redisConfiguration, EliteSetupReport report,
		CancellationToken ct) {
		redisConfiguration.AbortOnConnectFail = false;
		redisConfiguration.ConnectTimeout = 5000;
		redisConfiguration.SyncTimeout = 5000;

		try {
			using var redis = await ConnectionMultiplexer.ConnectAsync(redisConfiguration);
			using var registration = ct.Register(() => redis.Dispose());
			await redis.GetDatabase().PingAsync();
			report.AddInfo("Redis connectivity check passed.");
		}
		catch (Exception ex) {
			report.AddError(
				$"Redis is not reachable at {GetRedisEndpoint(redisConfiguration) ?? "(unknown endpoint)"}. {GetStartupHint("Redis")} Details: {ex.Message}");
		}
	}

	private void ValidateRequiredSetting(string value, string configKey, EliteSetupReport report) {
		if (string.IsNullOrWhiteSpace(value) || LooksLikePlaceholder(value)) {
			report.AddError(
				$"{configKey} is missing. Set it in appsettings.Development.json, user secrets, or environment variables for the local IDE flow, or root .env for full Docker Compose.");
		}
	}
	
	private void ValidateRequiredSetting(bool value, string configKey, EliteSetupReport report, bool expectedValue = true) {
		if (value != expectedValue) {
			report.AddError(
				$"{configKey} is not set to {expectedValue}. Set it in appsettings.Development.json, user secrets, or environment variables for the local IDE flow, or root .env for full Docker Compose.");
		}
	}

	private bool IsRunningInContainer() {
		return configuration.GetValue("DOTNET_RUNNING_IN_CONTAINER", false);
	}

	private string ResolveExpectedApiUrl() {
		if (IsRunningInContainer()) {
			var hostPort = configuration.GetValue("ELITE_API_PORT", 7008);
			return $"http://localhost:{hostPort}";
		}

		return "http://localhost:5164";
	}

	private string GetStartupHint(string dependencyName) {
		return IsRunningInContainer()
			? $"Start the full-stack flow with `docker compose --profile full-stack up -d` and confirm the {dependencyName} containers are healthy."
			: "Start the local infra with `docker compose up -d` before launching the API from Rider or `dotnet run`.";
	}

	private static bool IsLoopbackHost(string host) {
		return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
		       host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
		       host.Equals("::1", StringComparison.OrdinalIgnoreCase);
	}

	private static string? GetRedisHost(ConfigurationOptions configurationOptions) {
		return configurationOptions.EndPoints
			.OfType<DnsEndPoint>()
			.Select(endpoint => endpoint.Host)
			.FirstOrDefault()
		       ?? configurationOptions.EndPoints
			       .OfType<System.Net.IPEndPoint>()
			       .Select(endpoint => endpoint.Address.ToString())
			       .FirstOrDefault();
	}

	private static string? GetRedisEndpoint(ConfigurationOptions configurationOptions) {
		var dnsEndpoint = configurationOptions.EndPoints.OfType<DnsEndPoint>().FirstOrDefault();
		if (dnsEndpoint is not null) {
			return $"{dnsEndpoint.Host}:{dnsEndpoint.Port}";
		}

		var ipEndpoint = configurationOptions.EndPoints.OfType<System.Net.IPEndPoint>().FirstOrDefault();
		return ipEndpoint?.ToString();
	}

	private static bool LooksLikePlaceholder(string value) {
		return value.Contains("goes-here", StringComparison.OrdinalIgnoreCase) ||
		       value.Contains("change-me", StringComparison.OrdinalIgnoreCase) ||
		       value.Equals("token", StringComparison.OrdinalIgnoreCase);
	}
}
