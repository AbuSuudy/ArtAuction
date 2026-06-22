using ArtAuction.Contract;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace ArtAuction.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BidController(ILogger<BidController> logger, IBus bus) : ControllerBase
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


        [HttpPost("SubmitBid")]
        public async Task<IActionResult> SubmitBid(int id, decimal bid)
        {
            var artwork = ArtCollecetion.FirstOrDefault(x => x.Id == id);

            if (artwork == null)
            {
                return NotFound("Art Work Not Found");
            }

            var artWorkBid = new ArtWorkBid
            {
                Id = Guid.NewGuid(),
                Art = artwork,
                Bid = bid
            };

            await bus.Publish<ArtWorkBid>(artWorkBid);

            return Accepted(artWorkBid);
        }
    }
}
