using ArtAuction.Consumer;
using ArtAuction.Consumer.Activities;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit;
using OpenTelemetry;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<TableServiceClient>(sp =>
{
    var tablesConnectionString = builder.Configuration.GetConnectionString("tables");

    var serviceClient = tablesConnectionString is not null
        ? new TableServiceClient(tablesConnectionString)
        : new TableServiceClient(
            new Uri(builder.Configuration["StorageAccount:TableUri"]!),
            new DefaultAzureCredential());

    serviceClient.CreateTableIfNotExists("BidCompensation");

    serviceClient.CreateTableIfNotExists("BidStatus");

    return serviceClient;
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ArtWorkBidConsumer>();

    x.AddActivity<BidAcceptedActivity, BidAcceptedArguments, BidAcceptedLog>();
    x.AddActivity<BidRejectedActivity, BidRejectedArguments, BidRejectedLog>();
    x.AddActivity<AquiredActivity, AquiredArguments, AquiredLog>();

    var rabbitMQConnectionString = builder.Configuration.GetConnectionString("messaging");

    if (rabbitMQConnectionString is not null)
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
    .WithTracing(tracing => tracing.AddSource("MassTransit"))
    .WithMetrics(metrics => metrics.AddMeter("MassTransit"))
    .UseAzureMonitorExporter(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    })
    .UseOtlpExporter();

var host = builder.Build();
host.Run();
