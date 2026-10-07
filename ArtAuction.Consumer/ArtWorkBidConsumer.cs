using ArtAuction.Contract;
using Azure.Data.Tables;
using MassTransit;

namespace ArtAuction.Consumer
{
    internal class ArtWorkBidConsumer(TableServiceClient tableServiceClient) : IConsumer<ArtWorkBid>
    {
        public async Task Consume(ConsumeContext<ArtWorkBid> context)
        {
            Console.WriteLine($"Bid Submitted - Id : {context.Message.Id} Bid: {context.Message.Bid} Price: {context.Message.Art.Price}");

            var tableClient = tableServiceClient.GetTableClient("BidStatus");

            await tableClient.AddEntityAsync(new BidEntity(context.Message.Art.Id.ToString(), context.Message.Id.ToString())
            {
                Price = context.Message.Art.Price,
                Name = context.Message.Art.Name,
                ArtWork = context.Message.Art.ArtWork,
                Bid = (long)(context.Message.Bid * 100),
                Id = context.Message.Id,
                BidStaus = BidStaus.Submited
            });

            var builder = new RoutingSlipBuilder(NewId.NextGuid());

            if (context.Message.Bid >= context.Message.Art.Price * 0.8m)
            {
                builder.AddActivity("BidAccepted", new Uri("queue:bidaccepted_execute"), context.Message);

                builder.AddActivity("Aquired", new Uri("queue:aquired_execute"), context.Message);
            }
            else
            {
                builder.AddActivity("BidRejected", new Uri("queue:bidrejected_execute"), context.Message);
            }

            var routingSlipBuild = builder.Build();

            await context.Execute(routingSlipBuild).ConfigureAwait(false);

        }
    }
}