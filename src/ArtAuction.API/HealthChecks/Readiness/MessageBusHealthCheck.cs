using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.API.HealthChecks.Readiness
{
    public class MessageBusHealthCheck(IBusControl busControl) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var health = busControl.CheckHealth();

            var result = health.Status switch
            {
                BusHealthStatus.Healthy => HealthCheckResult.Healthy(health.Description),
                BusHealthStatus.Degraded => HealthCheckResult.Degraded(health.Description, health.Exception),
                _ => HealthCheckResult.Unhealthy(health.Description, health.Exception)
            };

            return Task.FromResult(result);
        }
    }
}
