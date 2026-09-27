namespace ArtAuction.Contract
{
    public record Art
    {
        public int Id { get; set; }

        public decimal Price { get; set; }

        public required string Name { get; set; }

        public required string ArtWork { get; set; }
    }

    public record ArtWorkBid
    {
        public Guid Id { get; set; }
        public Art Art { get; set; }
        public decimal Bid { get; set; }
    }
}