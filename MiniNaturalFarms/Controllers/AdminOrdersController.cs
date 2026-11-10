using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _orders;
    public AdminOrdersController(IAdminOrderService orders) => _orders = orders;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOrderSummaryDto>>> GetPaged([FromQuery] AdminOrderQuery query, CancellationToken ct) =>
        Ok(await _orders.GetPagedAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminOrderDto>> Get(int id, CancellationToken ct) =>
        Ok(await _orders.GetAsync(id, ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<AdminOrderDto>> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct) =>
        Ok(await _orders.UpdateStatusAsync(id, request, ct));

    [HttpPost("{id:int}/refund")]
    public async Task<ActionResult<AdminOrderDto>> Refund(int id, CancellationToken ct) =>
        Ok(await _orders.RefundAsync(id, ct));
}