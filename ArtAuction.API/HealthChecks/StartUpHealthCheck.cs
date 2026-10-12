using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.API.HealthChecks
{
    public class StartUpHealthCheck(IHostApplicationLifetime  hostApplicationLifetime) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return hostApplicationLifetime.ApplicationStarted.IsCancellationRequested
                ? HealthCheckResult.Healthy("The application has finished starting up.")
                : HealthCheckResult.Unhealthy("The application has not finished starting up.");
        }
    }
}
