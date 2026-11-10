using MiniNaturalFarms.Application.Orders;

namespace MiniNaturalFarms.Application.Admin;

public class CustomerQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record CustomerSummaryDto(
    string Id, string FullName, string Email, string? PhoneNumber, DateTime CreatedAt,
    int OrderCount, decimal TotalSpent, DateTime? LastOrderAt);

public record CustomerDetailDto(
    string Id, string FullName, string Email, string? PhoneNumber, DateTime CreatedAt,
    int OrderCount, decimal TotalSpent, DateTime? LastOrderAt, int AddressCount,
    List<OrderSummaryDto> RecentOrders);