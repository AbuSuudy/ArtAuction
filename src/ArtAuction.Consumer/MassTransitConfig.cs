using ArtAuction.Consumer.Activities;
using ArtAuction.Contract;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;
using static MassTransit.Logging.OperationName;

namespace ArtAuction.Consumer
{
    internal static class QueueNames
    {
        public const string ArtWorkBid = "art_work_bid";
        public const string BidAcceptedExecute = "bid_accepted_execute";
        public const string BidAcceptedCompensate = "bid_accepted_compensate";
        public const string BidRejectedExecute = "bid_rejected_execute";
        public const string BidRejectedCompensate = "bid_rejected_compensate";
        public const string AquiredExecute = "aquired_execute";
        public const string AquiredCompensate = "aquired_compensate";
    }

    internal static class MassTransitConfig
    {
        public static void ConfigureRoutingSlipEndpoints(IRegistrationContext context, IReceiveConfigurator cfg)
        {
            cfg.ReceiveEndpoint(QueueNames.ArtWorkBid, e =>
            {
                e.ConfigureConsumer<ArtWorkBidConsumer>(context);
            });

            cfg.ReceiveEndpoint(QueueNames.BidAcceptedExecute, execute =>
            {
                cfg.ReceiveEndpoint(QueueNames.BidAcceptedCompensate, compensate =>
                {
                    execute.ConfigureActivity(compensate, context, typeof(BidAcceptedActivity));
                });
            });

            cfg.ReceiveEndpoint(QueueNames.BidRejectedExecute, execute =>
            {
                cfg.ReceiveEndpoint(QueueNames.BidRejectedCompensate, compensate =>
                {
                    execute.ConfigureActivity(compensate, context, typeof(BidRejectedActivity));
                });
            });

            cfg.ReceiveEndpoint(QueueNames.AquiredExecute, execute =>
            {
                cfg.ReceiveEndpoint(QueueNames.AquiredCompensate, compensate =>
                {
                    execute.ConfigureActivity(compensate, context, typeof(AquiredActivity));
                });
            });
        }
    }
}
