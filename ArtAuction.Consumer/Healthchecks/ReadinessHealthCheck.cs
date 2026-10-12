using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Consumer.Healthchecks
{
    public class ReadinessHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
