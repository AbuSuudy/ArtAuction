using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Consumer.Endpoints
{
    public static class HealthEndpoints
    {
        public const string StartupTag = "startup";
        public const string LiveTag = "live";
        public const string ReadyTag = "ready";

        public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("health")
                .WithTags("Health");

            group.MapGet("/startup",
                (HealthCheckService healthCheckService, CancellationToken ct) =>
                    CheckHealth(healthCheckService, StartupTag, ct))
                .WithSummary("Startup probe")
                .WithDescription("Reports whether the app has finished starting up.");

            group.MapGet("/live",
                (HealthCheckService healthCheckService, CancellationToken ct) =>
                    CheckHealth(healthCheckService, LiveTag, ct))
                .WithSummary("Liveness probe")
                .WithDescription("Reports whether the app is still running correctly.");

            group.MapGet("/ready",
                (HealthCheckService healthCheckService, CancellationToken ct) =>
                    CheckHealth(healthCheckService, ReadyTag, ct))
                .WithSummary("Readiness probe")
                .WithDescription("Reports whether the app's dependencies are reachable.");

            return routes;
        }

        private static async Task<IResult> CheckHealth(HealthCheckService healthCheckService, string tag, CancellationToken cancellationToken = default)
        {
            var report = await healthCheckService.CheckHealthAsync(
                registration => registration.Tags.Contains(tag),
                cancellationToken);

            var body = new
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString())
            };

            var statusCode = report.Status == HealthStatus.Unhealthy
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status200OK;

            return Results.Json(body, statusCode: statusCode);
        }
    }
}
