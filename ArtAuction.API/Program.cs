using ArtAuction.API;
using ArtAuction.API.Endpoints;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using MassTransit;
using OpenTelemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

//Register globally so this policy can be reused
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.SubmitBid, policy => policy.RequireRole("Bid.Submit"));

builder.Services.AddMassTransit(x =>
{
    if (builder.Environment.IsDevelopment())
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(builder.Configuration.GetConnectionString("messaging")!));

            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(new Uri(builder.Configuration["ServiceBus:Host"]!));

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

app.MapBidEndpoints();

app.Run();
