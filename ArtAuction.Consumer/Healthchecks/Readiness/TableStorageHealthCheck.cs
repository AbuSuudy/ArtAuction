using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Consumer.Healthchecks.Readiness
{
    public class TableStorageHealthCheck(TableServiceClient tableServiceClient) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
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
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Table Storage is unreachable.", ex);
            }
        }
    }
}
