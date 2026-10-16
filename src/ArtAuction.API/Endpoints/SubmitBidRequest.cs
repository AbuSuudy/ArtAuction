using FluentValidation;

namespace ArtAuction.API.Endpoints
{
    public record SubmitBidRequest
    {
        public int Id { get; set; }

        public decimal Bid { get; set; }
    }

    public class SubmitBidRequestValidator : AbstractValidator<SubmitBidRequest>
    {
        public SubmitBidRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();           

            RuleFor(x => x.Bid)
                .NotEmpty()
                .GreaterThanOrEqualTo(1);
        }
    }
}
