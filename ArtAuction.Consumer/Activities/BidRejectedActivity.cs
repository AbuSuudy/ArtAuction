using ArtAuction.Contract;
using Azure.Data.Tables;
using MassTransit;

namespace ArtAuction.Consumer.Activities
{
    public class BidRejectedArguments
    {
        public Guid Id { get; set; }
        public Art Art { get; set; }
        public decimal Bid { get; set; }
    }

    public class BidRejectedLog
    {
        public ArtWorkBid ArtWorkBid { get; init; }
    }

    public class BidRejectedActivity(TableServiceClient client) : IActivity<BidRejectedArguments, BidRejectedLog>
    {
        public async Task<CompensationResult> Compensate(CompensateContext<BidRejectedLog> context)
        {
            var tableclient = client.GetTableClient("BidCompensation");

            await tableclient.AddEntityAsync(new BidCompensationEntity(context.Log.ArtWorkBid.Art.Name, context.Log.ArtWorkBid.Id.ToString())
            {
                Bid = (long)(context.Log.ArtWorkBid.Bid * 100),
                Id = context.Log.ArtWorkBid.Id,
                BidCompensationStaus = BidCompensationStaus.BidRejected_Compensated
            });

            Console.WriteLine("Bid Rejected Compensared");

            return context.Compensated();
        }

        async Task<ExecutionResult> IExecuteActivity<BidRejectedArguments>.Execute(ExecuteContext<BidRejectedArguments> context)
        {
            Console.WriteLine($"Rejected - Id : {context.Arguments.Id} Bid: {context.Arguments.Bid} Price: {context.Arguments.Art.Price}");

            var tableClient = client.GetTableClient("BidStatus");

            var response = await tableClient.GetEntityAsync<BidEntity>(partitionKey: context.Arguments.Art.Id.ToString(), rowKey: context.Arguments.Id.ToString());

            response.Value.BidStaus = BidStaus.Rejected;

            await tableClient.UpdateEntityAsync(
                response.Value,
                response.Value.ETag,
                TableUpdateMode.Replace);


            return context.Completed<BidRejectedLog>(new
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