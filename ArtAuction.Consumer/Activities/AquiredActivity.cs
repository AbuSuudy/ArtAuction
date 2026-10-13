using ArtAuction.Contract;
using Azure.Data.Tables;
using MassTransit;

namespace ArtAuction.Consumer.Activities
{
    public class AquiredArguments
    {
        public Guid Id { get; set; }
        public Art Art { get; set; }
        public decimal Bid { get; set; }
    }

    public class AquiredLog
    {
        public ArtWorkBid ArtWorkBid { get; init; }
    }

    public class AquiredActivity(TableServiceClient client, ILogger<AquiredActivity> logger) : IActivity<AquiredArguments, AquiredLog>
    {
        public async Task<CompensationResult> Compensate(CompensateContext<AquiredLog> context)
        {
            var tableclient = client.GetTableClient("BidCompensation");

            await tableclient.AddEntityAsync(new BidCompensationEntity(context.Log.ArtWorkBid.Art.Name, context.Log.ArtWorkBid.Id.ToString())
            {
                Bid = (long)(context.Log.ArtWorkBid.Bid * 100),
                Id = context.Log.ArtWorkBid.Id,
                BidCompensationStaus = BidCompensationStaus.Aquired_Compensated
            });

            logger.LogInformation("Aquired Compensared");

            return context.Compensated();
        }

        async Task<ExecutionResult> IExecuteActivity<AquiredArguments>.Execute(ExecuteContext<AquiredArguments> context)
        {

            logger.LogInformation($"Aquired - Id : {context.Arguments.Id} Bid: {context.Arguments.Bid} Price: {context.Arguments.Art.Price}");

            var tableClient = client.GetTableClient("BidStatus");

            var response = await tableClient.GetEntityAsync<BidEntity>(partitionKey: context.Arguments.Art.Id.ToString(), rowKey: context.Arguments.Id.ToString());

            response.Value.BidStaus = BidStaus.Aquired;

            await tableClient.UpdateEntityAsync(
                response.Value,
                response.Value.ETag,
                TableUpdateMode.Replace);

            return context.Completed<AquiredLog>(new
            {
                ArtWorkBid = new ArtWorkBid
                {
                    Art = context.Arguments.Art,
                    Bid = context.Arguments.Bid,
                    Id = context.Arguments.Id
                }
            });
        }
    }
}