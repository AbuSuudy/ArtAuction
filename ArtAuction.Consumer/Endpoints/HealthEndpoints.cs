using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Consumer.Endpoints
{
    public static class HealthEndpoints
    {
        public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("Health")
                .WithTags("Health");

            group.MapGet("/Startup",
                (HealthCheckService healthCheckService, CancellationToken ct) =>
                    CheckHealth(healthCheckService, "Startup", ct))
                .WithSummary("Startup probe")
                .WithDescription("Reports whether the app has finished starting up.");

            group.MapGet("/Liveness",
                (HealthCheckService healthCheckService, CancellationToken ct) =>
                    CheckHealth(healthCheckService, "Liveness", ct))
                .WithSummary("Liveness probe")
                .WithDescription("Reports whether the app is still running correctly.");

            group.MapGet("/Readiness",
                (HealthCheckService healthCheckService, CancellationToken ct) =>
                    CheckHealth(healthCheckService, "Readiness", ct))
                .WithSummary("Readiness probe")
                .WithDescription("Reports whether the app is ready to receive traffic.");

            return routes;
        }

        private static async Task<IResult> CheckHealth(HealthCheckService healthCheckService, string HealthCheckType, CancellationToken cancellationToken = default)
        {
            var report = await healthCheckService.CheckHealthAsync(
              registration => registration.Name.Contains(HealthCheckType),
              cancellationToken);

            return Results.Ok(report);
        }
    }
}


