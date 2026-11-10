using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IInvoiceService _invoices;

    public OrdersController(IOrderService orders, IInvoiceService invoices)
    {
        _orders = orders;
        _invoices = invoices;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResultDto>> Checkout(CheckoutRequest request, CancellationToken ct) =>
        Ok(await _orders.CheckoutAsync(User.GetUserId(), request, ct));

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        Ok(await _orders.GetMyOrdersAsync(User.GetUserId(), page, pageSize, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken ct) =>
        Ok(await _orders.GetMyOrderAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await _orders.CancelAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:int}/payment")]
    public async Task<ActionResult<RazorpayPaymentDto>> GetPayment(int id, CancellationToken ct) =>
        Ok(await _orders.GetPaymentAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:int}/payment/verify")]
    public async Task<ActionResult<OrderDto>> VerifyPayment(int id, VerifyPaymentRequest request, CancellationToken ct) =>
        Ok(await _orders.VerifyPaymentAsync(User.GetUserId(), id, request, ct));

    [HttpGet("{id:int}/invoice")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Invoice(int id, CancellationToken ct)
    {
        // Admins can download any invoice; customers only their own
        var ownerFilter = User.IsInRole("Admin") ? null : User.GetUserId();
        var file = await _invoices.GenerateAsync(id, ownerFilter, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }
}