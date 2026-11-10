using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController : ControllerBase
{
    private readonly IAdminInventoryService _inventory;
    public AdminInventoryController(IAdminInventoryService inventory) => _inventory = inventory;

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryItemDto>>> Get([FromQuery] InventoryQuery query, CancellationToken ct) =>
        Ok(await _inventory.GetAsync(query, ct));

    [HttpPut("{productId:int}/stock")]
    public async Task<ActionResult<InventoryItemDto>> SetStock(int productId, SetStockRequest request, CancellationToken ct) =>
        Ok(await _inventory.SetStockAsync(productId, request.Quantity, ct));
}