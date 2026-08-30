using ArtAuction.Consumer;
using ArtAuction.Consumer.Activities;
using Azure.Data.Tables;
using Azure.Identity;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<TableServiceClient>(sp =>
{
    var serviceClient = new TableServiceClient(
        new Uri("https://stmasstransit001.table.core.windows.net"),
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
        cfg.Host(new Uri("sb://sb-masstransit-neu-002.servicebus.windows.net"));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddApplicationInsightsTelemetry();

var host = builder.Build();
host.Run();
