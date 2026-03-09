using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EliteAPI.Setup;

public sealed class EliteSetupReadinessHealthCheck(IEliteSetupDoctor doctor) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
		CancellationToken cancellationToken = default) {
		var report = await doctor.CreateReportAsync(true, cancellationToken);
		if (report.HasErrors) {
			var description = string.Join(" | ", report.Messages
				.Where(message => message.Severity == EliteSetupSeverity.Error)
				.Select(message => message.Message));
			return HealthCheckResult.Unhealthy(description);
		}

		if (report.HasWarnings) {
			var description = string.Join(" | ", report.Messages
				.Where(message => message.Severity == EliteSetupSeverity.Warning)
				.Select(message => message.Message));
			return HealthCheckResult.Degraded(description);
		}

		return HealthCheckResult.Healthy("Setup validation and dependency checks passed.");
	}
}
