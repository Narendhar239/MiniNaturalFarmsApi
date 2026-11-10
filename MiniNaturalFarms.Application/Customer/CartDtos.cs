using System.ComponentModel.DataAnnotations;

namespace MiniNaturalFarms.Application.Customer;

public record CartItemDto(
    int Id, int ProductId, string NameEn, string NameTe, string? ImageUrl,
    decimal Price, decimal? DiscountPrice, decimal UnitPrice,
    int Quantity, decimal LineTotal, int StockQuantity, bool IsAvailable);

public record CartDto(List<CartItemDto> Items, int TotalQuantity, decimal SubTotal, bool HasUnavailableItems);

public class AddCartItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 99)] public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, 99)] public int Quantity { get; set; }
}