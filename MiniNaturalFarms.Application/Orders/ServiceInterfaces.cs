using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Application.Orders;

public interface IOrderService
{
    Task<CheckoutResultDto> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<OrderDto> GetMyOrderAsync(string userId, int orderId, CancellationToken ct = default);
    Task<OrderDto> CancelAsync(string userId, int orderId, CancellationToken ct = default);
    Task<RazorpayPaymentDto> GetPaymentAsync(string userId, int orderId, CancellationToken ct = default);
    Task<OrderDto> VerifyPaymentAsync(string userId, int orderId, VerifyPaymentRequest request, CancellationToken ct = default);

    /// <summary>Called by the Razorpay webhook (already signature-checked).</summary>
    Task MarkPaidByGatewayOrderAsync(string razorpayOrderId, string razorpayPaymentId, CancellationToken ct = default);

    /// <summary>Cancels unpaid online orders older than maxAge and puts the stock back.</summary>
    Task<int> CancelExpiredUnpaidOrdersAsync(TimeSpan maxAge, CancellationToken ct = default);
}

public interface IInvoiceService
{
    /// <param name="userId">Pass null to skip the ownership check (admins).</param>
    Task<InvoiceFile> GenerateAsync(int orderId, string? userId, CancellationToken ct = default);
}

public interface IPaymentGateway
{
    string KeyId { get; }
    Task<string> CreateOrderAsync(long amountPaise, string receipt, CancellationToken ct = default);
    Task<string> RefundAsync(string razorpayPaymentId, long amountPaise, string receipt, CancellationToken ct = default);
    bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string signature);
    bool VerifyWebhookSignature(string rawBody, string signature);
}