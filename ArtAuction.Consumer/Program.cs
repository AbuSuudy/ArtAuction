using ArtAuction.Consumer;
using ArtAuction.Consumer.Activities;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<TableServiceClient>(sp =>
{
    var serviceClient = new TableServiceClient(
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

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration["ServiceBus:Host"]!));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("MassTransit"))
    .WithMetrics(metrics => metrics.AddMeter("MassTransit"))
    .UseAzureMonitorExporter(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });

var host = builder.Build();
host.Run();
