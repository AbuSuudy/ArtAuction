using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Consumer.Healthchecks
{
    public class ReadinessHealthCheck(HealthCheckService healthCheckService, TableServiceClient tableServiceClient) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var massTransitTask =  MassTransitHealthAsync(context, cancellationToken);
            var storageTask =  BlobStorageHealthAsync(context, cancellationToken);

            Task.WaitAll(massTransitTask, storageTask);

            var results = new  HealthCheckResult[]
            {
                await massTransitTask,
                await storageTask
            };

            var errors = results
                .Where(x => x.Status != HealthStatus.Healthy)
                .Select(x => x.Exception?.Message ?? x.Description);

            return results.All(x => x.Status == HealthStatus.Healthy)
                ? HealthCheckResult.Healthy("All readiness checks passed.")
               :HealthCheckResult.Unhealthy(
                   $"One or more readiness checks failed: {string.Join("; ", errors)}",
                   results.Select(x => x.Exception).FirstOrDefault(e => e is not null));
        }

        public async Task<HealthCheckResult> MassTransitHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
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
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("masstransit exception", ex);
            }
        }


        public async Task<HealthCheckResult> BlobStorageHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var tableClient = tableServiceClient.GetTableClient("BidStatus");

                var query = tableClient.QueryAsync<TableEntity>(
                    maxPerPage: 1,
                    select: ["PartitionKey"],
                    cancellationToken: cancellationToken);

                await foreach (var _ in query.AsPages(pageSizeHint: 1))
                {
                    break;
                }

                return HealthCheckResult.Healthy();

            }catch(Exception ex)
            {
                return HealthCheckResult.Unhealthy("Blob Storage Table Exception", ex);
            }
        }
    }
}
