using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;
    public WishlistController(IWishlistService wishlist) => _wishlist = wishlist;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WishlistItemDto>>> Get(CancellationToken ct) =>
        Ok(await _wishlist.GetAsync(User.GetUserId(), ct));

    // Lightweight list for showing filled hearts on product cards
    [HttpGet("ids")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetIds(CancellationToken ct) =>
        Ok(await _wishlist.GetProductIdsAsync(User.GetUserId(), ct));

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add(int productId, CancellationToken ct)
    {
        await _wishlist.AddAsync(User.GetUserId(), productId, ct);
        return NoContent();
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        await _wishlist.RemoveAsync(User.GetUserId(), productId, ct);
        return NoContent();
    }
}