using ArtAuction.API;
using ArtAuction.API.Endpoints;
using ArtAuction.API.HealthChecks;
using ArtAuction.API.HealthChecks.Readiness;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenTelemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.SubmitBid, policy => policy.RequireRole("Bid.Submit"));

builder.Services.AddMassTransit(x =>
{
    var rabbitMQConnectionString = builder.Configuration.GetConnectionString("rabbit-mq");

    if (rabbitMQConnectionString is not null)
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(rabbitMQConnectionString));

            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(new Uri(builder.Configuration["ServiceBus:Host"]!), h =>
            {
                h.TokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = builder.Configuration["ManagedIdentity:ClientId"]
                });
            });

            cfg.ConfigureEndpoints(context);
        });
    }
});

builder.Services.AddOpenTelemetry()
.UseAzureMonitor(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
})
.UseOtlpExporter()
.WithTracing(tracing => tracing.AddSource("MassTransit"))
.WithMetrics(metrics => metrics.AddMeter("MassTransit"));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddCheck<StartUpHealthCheck>("Startup", tags: [HealthEndpoints.StartupTag])
    .AddCheck<LivenessHealthCheck>("Liveness", tags: [HealthEndpoints.LiveTag])
    .AddCheck<MessageBusHealthCheck>("MessageBus", tags: [HealthEndpoints.ReadyTag]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

app.MapHealthEndpoints();
app.MapBidEndpoints();

app.Run();
