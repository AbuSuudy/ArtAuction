using ArtAuction.Contract;
using Azure.Data.Tables;
using MassTransit;

namespace ArtAuction.Consumer
{
    internal class ArtWorkBidConsumer(TableServiceClient tableServiceClient, ILogger<ArtWorkBidConsumer> logger) : IConsumer<ArtWorkBid>
    {
        public async Task Consume(ConsumeContext<ArtWorkBid> context)
        {
            logger.LogInformation($"Bid Submitted - Id : {context.Message.Id} Bid: {context.Message.Bid} Price: {context.Message.Art.Price}");

            var tableClient = tableServiceClient.GetTableClient("BidStatus");

            await tableClient.AddEntityAsync(new BidEntity()
            {
                Price = context.Message.Art.Price,
                Name = context.Message.Art.Name,
                ArtWork = context.Message.Art.ArtWork,
                Bid = (long)(context.Message.Bid * 100),
                Id = context.Message.Id,
                BidStaus = BidStaus.Submited,
                PartitionKey = context.Message.Art.Id.ToString(),
                RowKey = context.Message.Id.ToString(), 
            });

            var builder = new RoutingSlipBuilder(NewId.NextGuid());

            if (context.Message.Bid >= context.Message.Art.Price * 0.8m)
            {
                builder.AddActivity("BidAccepted", new Uri($"queue:{QueueNames.BidAcceptedExecute}"), context.Message);

                builder.AddActivity("Aquired", new Uri($"queue:{QueueNames.AquiredExecute}"), context.Message);
            }
            else
            {
                builder.AddActivity("BidRejected", new Uri($"queue:{QueueNames.BidRejectedExecute}"), context.Message);
            }

            var routingSlipBuild = builder.Build();

            await context.Execute(routingSlipBuild).ConfigureAwait(false);

        }
    }
}