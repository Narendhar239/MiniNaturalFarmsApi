using System.ComponentModel.DataAnnotations;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Domain.Enums;

namespace MiniNaturalFarms.Application.Admin;

public class AdminOrderQuery
{
    public string? Search { get; set; }
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UpdateOrderStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
}

public record AdminOrderSummaryDto(
    int Id, string OrderNumber, DateTime CreatedAt,
    string CustomerName, string CustomerEmail,
    string Status, string PaymentMethod, string PaymentStatus,
    decimal TotalAmount, int ItemCount);

public record AdminOrderDto(
    int Id, string OrderNumber, DateTime CreatedAt,
    string Status, string PaymentMethod, string PaymentStatus,
    decimal SubTotal, decimal ShippingCharge, decimal TotalAmount,
    string CustomerEmail,
    string ShipFullName, string ShipPhone, string ShipAddress,
    string ShipCity, string ShipState, string ShipPincode,
    string? RazorpayOrderId, string? RazorpayPaymentId, string? RazorpayRefundId, DateTime? RefundedAt,
    List<OrderItemDto> Items, List<string> AllowedNextStatuses);