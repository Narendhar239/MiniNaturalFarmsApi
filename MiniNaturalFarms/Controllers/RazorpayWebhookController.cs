using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Orders;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/payments/razorpay")]
[AllowAnonymous]
public class RazorpayWebhookController : ControllerBase
{
    private readonly IPaymentGateway _gateway;
    private readonly IOrderService _orders;

    public RazorpayWebhookController(IPaymentGateway gateway, IOrderService orders)
    {
        _gateway = gateway;
        _orders = orders;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        if (!_gateway.VerifyWebhookSignature(body, signature))
            return BadRequest("Invalid signature.");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var eventName = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
        if (eventName is "payment.captured" or "order.paid"
            && root.TryGetProperty("payload", out var payload)
            && payload.TryGetProperty("payment", out var payment)
            && payment.TryGetProperty("entity", out var entity))
        {
            var orderId = entity.TryGetProperty("order_id", out var o) ? o.GetString() : null;
            var paymentId = entity.TryGetProperty("id", out var p) ? p.GetString() : null;

            if (!string.IsNullOrEmpty(orderId) && !string.IsNullOrEmpty(paymentId))
                await _orders.MarkPaidByGatewayOrderAsync(orderId, paymentId, ct);
        }

        return Ok();
    }
}