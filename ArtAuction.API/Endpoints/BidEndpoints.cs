using ArtAuction.Contract;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ArtAuction.API.Endpoints
{
    public static class BidEndpoints
    {
        private static readonly List<Art> ArtCollecetion =
        [
            new Art {
                Id = 1,
                Price = 500_000,
                Name = "Folio from the \"Blue Qur'an",
                ArtWork = "https://www.metmuseum.org/art/collection/search/454662"
            },
            new Art {
                Id = 2,
                Price = 100_000,
                Name = "Pendant mask of Ìyọ́bà Idià",
                ArtWork = "https://www.metmuseum.org/art/collection/search/318622"
            }
        ];

        public static IEndpointRouteBuilder MapBidEndpoints(this IEndpointRouteBuilder routes, IConfiguration configuration)
        {
            var group = routes.MapGroup("api/Bid")
                .WithTags("Bid");

            //lambda format 
            /*group.MapPost("SubmitBid", async Task<Results<Accepted<ArtWorkBid>, NotFound<string>>> (int id, decimal bid, IBus bus) =>
            {
                var artwork = ArtCollecetion.FirstOrDefault(x => x.Id == id);

                if (artwork == null)
                {
                    return TypedResults.NotFound("Art Work Not Found");
                }

                var artWorkBid = new ArtWorkBid
                {
                    Id = Guid.NewGuid(),
                    Art = artwork,
                    Bid = bid
                };

                await bus.Publish<ArtWorkBid>(artWorkBid);

                return TypedResults.Accepted((string?)null, artWorkBid);
            });*/

            var submitBid = group.MapPost("SubmitBid", SubmitBid);

            if (!configuration.GetValue<bool>("DisableAuthForLocalDev"))
            {
                submitBid.RequireAuthorization(AuthorizationPolicies.SubmitBid);
            }

            return routes;
        }

        private static async Task<Results<Accepted<ArtWorkBid>, NotFound<string>>> SubmitBid(int id, decimal bid, IBus bus)
        {
            var artwork = ArtCollecetion.FirstOrDefault(x => x.Id == id);

            if (artwork is null)
            {
                return TypedResults.NotFound("Art Work Not Found");
            }

            var artWorkBid = new ArtWorkBid
            {
                Id = Guid.NewGuid(),
                Art = artwork,
                Bid = bid
            };

            await bus.Publish<ArtWorkBid>(artWorkBid);

            return TypedResults.Accepted((string?)null, artWorkBid);
        }
    }
}
