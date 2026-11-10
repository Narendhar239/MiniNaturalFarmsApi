using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MiniNaturalFarms.Application.Orders;

[JsonConverter(typeof(JsonStringEnumConverter<CheckoutPaymentMethod>))]
public enum CheckoutPaymentMethod
{
    Cod = 0,
    Online = 1
}

public class CheckoutRequest
{
    [Range(1, int.MaxValue)] public int AddressId { get; set; }
    public CheckoutPaymentMethod PaymentMethod { get; set; }
}

public class VerifyPaymentRequest
{
    [Required] public string RazorpayOrderId { get; set; } = string.Empty;
    [Required] public string RazorpayPaymentId { get; set; } = string.Empty;
    [Required] public string RazorpaySignature { get; set; } = string.Empty;
}

public record OrderItemDto(
    int ProductId, string ProductName, string? ImageUrl,
    decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderDto(
    int Id, string OrderNumber, DateTime CreatedAt,
    string Status, string PaymentMethod, string PaymentStatus,
    decimal SubTotal, decimal ShippingCharge, decimal TotalAmount,
    string ShipFullName, string ShipPhone, string ShipAddress,
    string ShipCity, string ShipState, string ShipPincode,
    List<OrderItemDto> Items);

public record OrderSummaryDto(
    int Id, string OrderNumber, DateTime CreatedAt,
    string Status, string PaymentMethod, string PaymentStatus,
    decimal TotalAmount, int ItemCount);

/// <summary>Everything the React app needs to open Razorpay Checkout.</summary>
public record RazorpayPaymentDto(
    string KeyId, string RazorpayOrderId, long AmountPaise, string Currency, string OrderNumber);

public record CheckoutResultDto(OrderDto Order, RazorpayPaymentDto? Payment);

public record InvoiceFile(byte[] Content, string FileName);