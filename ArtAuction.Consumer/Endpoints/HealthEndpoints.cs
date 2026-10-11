using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Consumer.Endpoints
{
    public static class HealthEndpoints
    {
        public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("Health")
                .WithTags("Health");
            //.ExcludeFromDescription();

            group.MapGet("/Startup", CheckHealth)
                .WithSummary("Startup probe")
                .WithDescription("Reports whether the app has finished starting up.");

            group.MapGet("/Liveness", CheckHealth)
                .WithSummary("Liveness probe")
                .WithDescription("Reports whether the app is still running correctly.");

            group.MapGet("/Readiness", CheckHealth)
                .WithSummary("Readiness probe")
                .WithDescription("Reports whether the app is ready to receive traffic.");

            return routes;
        }

        private static async Task<IResult> CheckHealth(HealthCheckService healthCheckService)
        {
            var report = await healthCheckService.CheckHealthAsync();

            return report.Status == HealthStatus.Healthy
                ? Results.Ok(report.Status.ToString())
                : Results.Json(report.Status.ToString(), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}


