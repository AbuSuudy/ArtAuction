using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.API.HealthChecks
{
    public class ReadinessHealthCheck(HealthCheckService healthCheckService) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var report = await healthCheckService.CheckHealthAsync(
                  registration => registration.Name.Contains("masstransit"),
                  cancellationToken);

            return report.Status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(),
                HealthStatus.Degraded => HealthCheckResult.Degraded(),
                _ => HealthCheckResult.Unhealthy()
            };
        }
    }
}
