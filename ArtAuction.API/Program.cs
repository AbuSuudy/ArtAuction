using ArtAuction.API;
using ArtAuction.API.Endpoints;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration["ServiceBus:Host"]!));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    })
    .WithTracing(tracing => tracing.AddSource("MassTransit"))
    .WithMetrics(metrics => metrics.AddMeter("MassTransit"));

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;

        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        var logger = context.RequestServices
         .GetRequiredService<ILogger<Program>>();

        logger.LogError(exception, exception?.Message);

        var dev = app.Environment.IsDevelopment();

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = exception?.Message,
            Status = 500
        });
    });
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();

    app.MapScalarApiReference();

//}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();



app.MapBidEndpoints();

app.Run();
