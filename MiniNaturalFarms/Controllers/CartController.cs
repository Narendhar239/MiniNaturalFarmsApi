using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;
    public CartController(ICartService cart) => _cart = cart;

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct) =>
        Ok(await _cart.GetAsync(User.GetUserId(), ct));

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        Ok(await _cart.AddItemAsync(User.GetUserId(), request, ct));

    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> SetQuantity(int productId, UpdateCartItemRequest request, CancellationToken ct) =>
        Ok(await _cart.SetQuantityAsync(User.GetUserId(), productId, request.Quantity, ct));

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int productId, CancellationToken ct) =>
        Ok(await _cart.RemoveItemAsync(User.GetUserId(), productId, ct));

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _cart.ClearAsync(User.GetUserId(), ct);
        return NoContent();
    }
}