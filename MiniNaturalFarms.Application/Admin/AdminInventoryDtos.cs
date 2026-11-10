using System.ComponentModel.DataAnnotations;

namespace MiniNaturalFarms.Application.Admin;

public static class InventoryRules
{
    public const int DefaultLowStockThreshold = 10;
}

public enum StockFilter
{
    All = 0,
    Low = 1,
    Out = 2
}

public class InventoryQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public StockFilter Filter { get; set; } = StockFilter.All;
    public int LowStockThreshold { get; set; } = InventoryRules.DefaultLowStockThreshold;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SetStockRequest
{
    [Range(0, 1000000)] public int Quantity { get; set; }
}

public record InventoryItemDto(
    int ProductId, string NameEn, string NameTe, string CategoryNameEn, string? ImageUrl,
    decimal Price, decimal? DiscountPrice, int StockQuantity, bool IsActive, string StockStatus);