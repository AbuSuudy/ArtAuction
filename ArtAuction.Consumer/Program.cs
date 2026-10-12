using ArtAuction.Consumer;
using ArtAuction.Consumer.Activities;
using ArtAuction.Consumer.Endpoints;
using ArtAuction.Consumer.Healthchecks;
using ArtAuction.Consumer.Healthchecks.Readiness;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddHealthChecks()
    .AddCheck<StartUpHealthCheck>("Startup", tags: [HealthEndpoints.StartupTag])
    .AddCheck<LivenessHealthCheck>("Liveness", tags: [HealthEndpoints.LiveTag])
    .AddCheck<MessageBusHealthCheck>("MessageBus", tags: [HealthEndpoints.ReadyTag])
    // Bounded below the 5s probe timeout; otherwise the Azure SDK's retries make an outage take ~15s to report.
    .AddCheck<TableStorageHealthCheck>("TableStorage", tags: [HealthEndpoints.ReadyTag], timeout: TimeSpan.FromSeconds(3));

var managedIdentityCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = builder.Configuration["ManagedIdentity:ClientId"]
});

builder.Services.AddSingleton<TableServiceClient>(sp =>
{
    var tablesConnectionString = builder.Configuration.GetConnectionString("tables");

    return tablesConnectionString is not null
        ? new TableServiceClient(tablesConnectionString)
        : new TableServiceClient(
            new Uri(builder.Configuration["StorageAccount:TableUri"]!),
            managedIdentityCredential);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ArtWorkBidConsumer>();

    x.AddActivity<BidAcceptedActivity, BidAcceptedArguments, BidAcceptedLog>();
    x.AddActivity<BidRejectedActivity, BidRejectedArguments, BidRejectedLog>();
    x.AddActivity<AquiredActivity, AquiredArguments, AquiredLog>();

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
                h.TokenCredential = managedIdentityCredential;
            });

            cfg.ConfigureEndpoints(context);
        });
    }
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("MassTransit"))
    .WithMetrics(metrics => metrics.AddMeter("MassTransit"))
    .UseAzureMonitorExporter(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    })
    .UseOtlpExporter();

var app = builder.Build();

var tableServiceClient = app.Services.GetRequiredService<TableServiceClient>();
await tableServiceClient.CreateTableIfNotExistsAsync("BidCompensation");
await tableServiceClient.CreateTableIfNotExistsAsync("BidStatus");

app.MapHealthEndpoints();

app.Run();
