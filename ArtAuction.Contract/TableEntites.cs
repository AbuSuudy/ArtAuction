using Azure;
using Azure.Data.Tables;

namespace ArtAuction.Contract
{
    public enum BidStaus
    {
        Submited,
        Accepted,
        Rejected,
        Aquired,
        Failed
    }

    public record BidEntity : ITableEntity
    {
        public BidEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public BidEntity() { }

        public Guid Id { get; set; }
        public decimal Price { get; set; }

        public required string Name { get; set; }

        public required string ArtWork { get; set; }

        // store in pence
        public long Bid { get; set; }

        public BidStaus BidStaus { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    public enum BidCompensationStaus
    {
        Aquired_Compensated,
        BidAccepted_Compensated,
        BidRejected_Compensated,
    }

    public record BidCompensationEntity : ITableEntity
    {
        public BidCompensationEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public BidCompensationEntity() { }

        public Guid Id { get; set; }

        // store in pence
        public long Bid { get; set; }

        public BidCompensationStaus BidCompensationStaus { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
